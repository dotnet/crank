// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Azure;
using Azure.Core;
using Azure.Identity;
using Azure.ResourceManager;
using Azure.ResourceManager.Compute;
using Azure.ResourceManager.Compute.Models;
using Azure.ResourceManager.Network;
using Azure.ResourceManager.Network.Models;
using Azure.ResourceManager.Resources;
using AzureLocation = Azure.Core.AzureLocation;

namespace Microsoft.Crank.Controller.Provisioning
{
    /// <summary>
    /// Provisions Azure VMs with crank-agent installed and running.
    /// Uses Azure Resource Manager SDK for VM lifecycle management.
    /// </summary>
    public class AzureProvisioner : IInfrastructureProvisioner
    {
        private const string ResourceGroupPrefix = "rg-crank-";
        private const string TagSessionId = "crank-session";
        private const string TagCreatedAt = "crank-created-at";
        private const string TagAutoDeleteAfter = "crank-auto-delete-after";
        private const string TagManagedBy = "crank-managed-by";
        private const string TagPoolName = "crank-pool";

        private static readonly TimeSpan DefaultAutoDeleteAfter = TimeSpan.FromHours(2);

        private readonly ArmClient _armClient;
        private readonly SubscriptionResource _subscription;
        private readonly HttpClient _httpClient;
        private readonly Dictionary<string, string> _sessionResourceGroups = new();

        public AzureProvisioner(TokenCredential credential, string subscriptionId = null)
        {
            _armClient = new ArmClient(credential);

            if (!string.IsNullOrEmpty(subscriptionId))
            {
                _subscription = _armClient.GetSubscriptionResource(
                    SubscriptionResource.CreateResourceIdentifier(subscriptionId));
            }

            var handler = new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
            };
            _httpClient = new HttpClient(handler) { Timeout = TimeSpan.FromSeconds(10) };
        }

        /// <inheritdoc/>
        public async Task<IReadOnlyList<ProvisionedAgent>> ProvisionAsync(
            string sessionId,
            IDictionary<string, ProvisioningConfig> agentConfigs,
            CancellationToken cancellationToken = default)
        {
            return await ProvisionAsync(sessionId, agentConfigs, poolName: null, poolTtl: null, cancellationToken);
        }

        /// <summary>
        /// Provisions infrastructure with optional pool naming for reuse across runs.
        /// </summary>
        public async Task<IReadOnlyList<ProvisionedAgent>> ProvisionAsync(
            string sessionId,
            IDictionary<string, ProvisioningConfig> agentConfigs,
            string poolName,
            TimeSpan? poolTtl,
            CancellationToken cancellationToken = default)
        {
            var agents = new List<ProvisionedAgent>();
            var subscription = await GetSubscriptionAsync(cancellationToken);

            // When using a pool, name the resource group deterministically so it can be found later
            var resourceGroupName = !string.IsNullOrEmpty(poolName)
                ? $"{ResourceGroupPrefix}pool-{poolName}"
                : agentConfigs.Values.FirstOrDefault()?.ResourceGroup
                    ?? $"{ResourceGroupPrefix}{sessionId}";

            _sessionResourceGroups[sessionId] = resourceGroupName;

            // Determine the region from the first config (all VMs in same RG share region)
            var region = agentConfigs.Values.First().Region;

            Log.Write($"Provisioning infrastructure in resource group '{resourceGroupName}' in region '{region}'...");

            // Create resource group
            var rgData = new ResourceGroupData(new AzureLocation(region));
            rgData.Tags.Add(TagSessionId, sessionId);
            rgData.Tags.Add(TagCreatedAt, DateTime.UtcNow.ToString("o"));
            rgData.Tags.Add(TagManagedBy, "crank-controller");

            // Set auto-delete based on pool TTL or default
            var autoDeleteAfter = poolTtl ?? DefaultAutoDeleteAfter;
            rgData.Tags.Add(TagAutoDeleteAfter, DateTime.UtcNow.Add(autoDeleteAfter).ToString("o"));

            if (!string.IsNullOrEmpty(poolName))
            {
                rgData.Tags.Add(TagPoolName, poolName);
            }

            var rgCollection = subscription.GetResourceGroups();
            var rgOperation = await rgCollection.CreateOrUpdateAsync(
                WaitUntil.Completed, resourceGroupName, rgData, cancellationToken);
            var resourceGroup = rgOperation.Value;

            Log.Write($"Resource group '{resourceGroupName}' created.");

            // Create shared networking resources
            var (vnet, nsg) = await CreateNetworkingAsync(resourceGroup, region, agentConfigs, cancellationToken);

            // Provision VMs for each service in parallel
            var provisionTasks = new List<Task<List<ProvisionedAgent>>>();

            foreach (var (serviceName, config) in agentConfigs)
            {
                provisionTasks.Add(ProvisionServiceVmsAsync(
                    resourceGroup, vnet, nsg, serviceName, config, region, sessionId, cancellationToken));
            }

            var results = await Task.WhenAll(provisionTasks);

            foreach (var result in results)
            {
                agents.AddRange(result);
            }

            Log.Write($"Provisioned {agents.Count} agent(s) across {agentConfigs.Count} service(s).");
            return agents;
        }

        /// <inheritdoc/>
        public async Task TeardownAsync(string sessionId, CancellationToken cancellationToken = default)
        {
            if (!_sessionResourceGroups.TryGetValue(sessionId, out var resourceGroupName))
            {
                Log.Write($"No resource group found for session '{sessionId}', skipping teardown.");
                return;
            }

            try
            {
                var subscription = await GetSubscriptionAsync(cancellationToken);
                var rgResource = subscription.GetResourceGroups()
                    .GetIfExists(resourceGroupName, cancellationToken)?.Value;

                if (rgResource != null)
                {
                    Log.Write($"Tearing down resource group '{resourceGroupName}'...");
                    await rgResource.DeleteAsync(WaitUntil.Started, cancellationToken: cancellationToken);
                    Log.Write($"Resource group '{resourceGroupName}' deletion initiated.");
                }
                else
                {
                    Log.Write($"Resource group '{resourceGroupName}' not found, nothing to tear down.");
                }
            }
            catch (Exception ex)
            {
                Log.WriteWarning($"Warning: Failed to tear down resource group '{resourceGroupName}': {ex.Message}");
            }
            finally
            {
                _sessionResourceGroups.Remove(sessionId);
            }
        }

        /// <inheritdoc/>
        public async Task<bool> WaitForAgentsReadyAsync(
            IReadOnlyList<ProvisionedAgent> agents,
            TimeSpan timeout,
            CancellationToken cancellationToken = default)
        {
            var deadline = DateTime.UtcNow + timeout;
            var retryDelay = TimeSpan.FromSeconds(5);
            var maxRetryDelay = TimeSpan.FromSeconds(30);

            var pendingAgents = new HashSet<ProvisionedAgent>(agents);

            while (pendingAgents.Count > 0 && DateTime.UtcNow < deadline)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var checkTasks = pendingAgents.Select(async agent =>
                {
                    try
                    {
                        var response = await _httpClient.GetAsync(
                            new Uri(agent.EndpointUri, "/jobs/info"), cancellationToken);

                        if (response.IsSuccessStatusCode)
                        {
                            return (agent, ready: true);
                        }
                    }
                    catch
                    {
                        // Agent not ready yet
                    }

                    return (agent, ready: false);
                }).ToList();

                var results = await Task.WhenAll(checkTasks);

                foreach (var (agent, ready) in results)
                {
                    if (ready)
                    {
                        Log.Write($"Agent '{agent.ServiceName}' at {agent.EndpointUri} is ready.");
                        pendingAgents.Remove(agent);
                    }
                }

                if (pendingAgents.Count > 0)
                {
                    var remaining = deadline - DateTime.UtcNow;
                    Log.Write($"Waiting for {pendingAgents.Count} agent(s) to become ready... ({remaining.TotalSeconds:F0}s remaining)");
                    await Task.Delay(retryDelay, cancellationToken);

                    // Exponential backoff capped at maxRetryDelay
                    retryDelay = TimeSpan.FromSeconds(Math.Min(retryDelay.TotalSeconds * 1.5, maxRetryDelay.TotalSeconds));
                }
            }

            if (pendingAgents.Count > 0)
            {
                foreach (var agent in pendingAgents)
                {
                    Log.WriteError($"Agent '{agent.ServiceName}' at {agent.EndpointUri} failed to become ready within timeout.");
                }
                return false;
            }

            return true;
        }

        /// <inheritdoc/>
        public async Task<int> CleanupOrphanedResourcesAsync(
            TimeSpan maxAge,
            CancellationToken cancellationToken = default)
        {
            var subscription = await GetSubscriptionAsync(cancellationToken);
            var cutoff = DateTime.UtcNow - maxAge;
            var cleaned = 0;

            await foreach (var rg in subscription.GetResourceGroups().GetAllAsync(cancellationToken: cancellationToken))
            {
                if (rg.Data.Tags.TryGetValue(TagManagedBy, out var managedBy) && managedBy == "crank-controller")
                {
                    if (rg.Data.Tags.TryGetValue(TagCreatedAt, out var createdAtStr)
                        && DateTime.TryParse(createdAtStr, out var createdAt)
                        && createdAt < cutoff)
                    {
                        Log.Write($"Cleaning up orphaned resource group '{rg.Data.Name}' (created {createdAt:u})...");
                        await rg.DeleteAsync(WaitUntil.Started, cancellationToken: cancellationToken);
                        cleaned++;
                    }
                }
            }

            return cleaned;
        }

        /// <summary>
        /// Cleans up crank-managed resource groups that have passed their auto-delete-after time.
        /// Called automatically at the start of each provisioned run to prevent cost accumulation.
        /// </summary>
        public async Task<int> CleanupExpiredResourcesAsync(CancellationToken cancellationToken = default)
        {
            var subscription = await GetSubscriptionAsync(cancellationToken);
            var now = DateTime.UtcNow;
            var cleaned = 0;

            await foreach (var rg in subscription.GetResourceGroups().GetAllAsync(cancellationToken: cancellationToken))
            {
                if (rg.Data.Tags.TryGetValue(TagManagedBy, out var managedBy) && managedBy == "crank-controller"
                    && rg.Data.Tags.TryGetValue(TagAutoDeleteAfter, out var autoDeleteStr)
                    && DateTime.TryParse(autoDeleteStr, out var autoDeleteAt)
                    && now > autoDeleteAt)
                {
                    Log.Write($"Auto-cleaning expired resource group '{rg.Data.Name}' (expired {autoDeleteAt:u})...");
                    await rg.DeleteAsync(WaitUntil.Started, cancellationToken: cancellationToken);
                    cleaned++;
                }
            }

            return cleaned;
        }

        /// <inheritdoc/>
        public async Task<IReadOnlyList<ProvisionedAgent>> FindExistingPoolAsync(
            string poolName,
            IDictionary<string, ProvisioningConfig> agentConfigs,
            CancellationToken cancellationToken = default)
        {
            return await FindExistingPoolAsync(poolName, agentConfigs, maxPoolAge: null, cancellationToken);
        }

        /// <summary>
        /// Searches for an existing pool, rejecting it if it exceeds the maximum age.
        /// </summary>
        public async Task<IReadOnlyList<ProvisionedAgent>> FindExistingPoolAsync(
            string poolName,
            IDictionary<string, ProvisioningConfig> agentConfigs,
            TimeSpan? maxPoolAge,
            CancellationToken cancellationToken = default)
        {
            var subscription = await GetSubscriptionAsync(cancellationToken);
            var expectedRgName = $"{ResourceGroupPrefix}pool-{poolName}";

            Log.Write($"Searching for existing pool '{poolName}' (resource group '{expectedRgName}')...");

            ResourceGroupResource resourceGroup;
            try
            {
                var rgResponse = await subscription.GetResourceGroups()
                    .GetAsync(expectedRgName, cancellationToken);
                resourceGroup = rgResponse.Value;
            }
            catch (RequestFailedException ex) when (ex.Status == 404)
            {
                Log.Write($"No existing pool '{poolName}' found.");
                return null;
            }

            // Verify it's a crank-managed pool
            if (!resourceGroup.Data.Tags.TryGetValue(TagManagedBy, out var managedBy) || managedBy != "crank-controller")
            {
                Log.Write($"Resource group '{expectedRgName}' exists but is not crank-managed. Skipping.");
                return null;
            }

            if (!resourceGroup.Data.Tags.TryGetValue(TagPoolName, out var taggedPool) || taggedPool != poolName)
            {
                Log.Write($"Resource group '{expectedRgName}' exists but has wrong pool tag. Skipping.");
                return null;
            }

            // Check if it has expired
            if (resourceGroup.Data.Tags.TryGetValue(TagAutoDeleteAfter, out var autoDeleteStr)
                && DateTime.TryParse(autoDeleteStr, out var autoDeleteAt)
                && DateTime.UtcNow > autoDeleteAt)
            {
                Log.Write($"Pool '{poolName}' has expired (auto-delete was {autoDeleteAt:u}). Will provision fresh.");
                return null;
            }

            // Check if pool exceeds max age (default: 24 hours)
            if (resourceGroup.Data.Tags.TryGetValue(TagCreatedAt, out var createdAtStr)
                && DateTime.TryParse(createdAtStr, out var poolCreatedAt))
            {
                var poolAge = DateTime.UtcNow - poolCreatedAt;
                var maxAge = maxPoolAge ?? TimeSpan.FromHours(24);

                if (poolAge > maxAge)
                {
                    Log.Write($"Pool '{poolName}' is {poolAge.TotalHours:F1} hours old (max: {maxAge.TotalHours:F1}h). Will provision fresh.");
                    // Initiate deletion of the stale pool
                    try
                    {
                        await resourceGroup.DeleteAsync(WaitUntil.Started, cancellationToken: cancellationToken);
                        Log.Write($"Initiated deletion of stale pool resource group '{expectedRgName}'.");
                    }
                    catch (Exception ex)
                    {
                        Log.WriteWarning($"Failed to delete stale pool '{expectedRgName}': {ex.Message}");
                    }
                    return null;
                }

                Log.Write($"Pool '{poolName}' age: {poolAge.TotalHours:F1} hours (max: {maxAge.TotalHours:F1}h).");
            }

            // Discover VMs and build ProvisionedAgent list from existing resources
            var agents = new List<ProvisionedAgent>();

            await foreach (var vm in resourceGroup.GetVirtualMachines().GetAllAsync(cancellationToken: cancellationToken))
            {
                // VM naming convention: vm-{serviceName}-{index}
                var vmName = vm.Data.Name;
                var parts = vmName.Split('-');
                // Expect "vm-{serviceName}-{index}", serviceName may contain hyphens
                string serviceName = null;
                if (parts.Length >= 3 && parts[0] == "vm")
                {
                    // Rejoin everything between first and last dash segment
                    serviceName = string.Join("-", parts.Skip(1).Take(parts.Length - 2));
                }

                if (serviceName == null || !agentConfigs.ContainsKey(serviceName))
                {
                    continue;
                }

                var config = agentConfigs[serviceName];

                // Find the public IP of this VM through its NIC
                string publicIp = null;
                foreach (var nicRef in vm.Data.NetworkProfile.NetworkInterfaces)
                {
                    try
                    {
                        var nicResource = _armClient.GetNetworkInterfaceResource(nicRef.Id);
                        var nic = await nicResource.GetAsync(cancellationToken: cancellationToken);
                        foreach (var ipConfig in nic.Value.Data.IPConfigurations)
                        {
                            if (ipConfig.PublicIPAddress?.Id != null)
                            {
                                var pipResource = _armClient.GetPublicIPAddressResource(ipConfig.PublicIPAddress.Id);
                                var pip = await pipResource.GetAsync(cancellationToken: cancellationToken);
                                publicIp = pip.Value.Data.IPAddress;
                                break;
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Log.WriteWarning($"Could not resolve IP for VM '{vmName}': {ex.Message}");
                    }

                    if (publicIp != null) break;
                }

                if (publicIp == null)
                {
                    Log.WriteWarning($"Could not find public IP for VM '{vmName}' in pool '{poolName}'. Skipping.");
                    continue;
                }

                agents.Add(new ProvisionedAgent
                {
                    EndpointUri = new Uri($"http://{publicIp}:{config.AgentPort}"),
                    IpAddress = publicIp,
                    Hostname = vmName,
                    ResourceGroupName = expectedRgName,
                    VmName = vmName,
                    ServiceName = serviceName
                });
            }

            if (agents.Count == 0)
            {
                Log.Write($"Pool '{poolName}' resource group exists but has no matching VMs.");
                return null;
            }

            // Verify we have the expected number of agents per service
            foreach (var (serviceName, config) in agentConfigs)
            {
                var serviceAgents = agents.Count(a => a.ServiceName == serviceName);
                if (serviceAgents < config.Count)
                {
                    Log.Write($"Pool '{poolName}' has {serviceAgents} agent(s) for service '{serviceName}', expected {config.Count}. Will provision fresh.");
                    return null;
                }
            }

            Log.Write($"Found existing pool '{poolName}' with {agents.Count} agent(s).");
            return agents;
        }

        /// <inheritdoc/>
        public async Task ExtendPoolTtlAsync(
            string poolName,
            TimeSpan ttl,
            CancellationToken cancellationToken = default)
        {
            var subscription = await GetSubscriptionAsync(cancellationToken);
            var expectedRgName = $"{ResourceGroupPrefix}pool-{poolName}";

            try
            {
                var rgResponse = await subscription.GetResourceGroups()
                    .GetAsync(expectedRgName, cancellationToken);
                var resourceGroup = rgResponse.Value;

                var newExpiry = DateTime.UtcNow.Add(ttl);

                // Update tags by re-applying the resource group with updated tag values
                var rgData = new ResourceGroupData(resourceGroup.Data.Location);
                foreach (var tag in resourceGroup.Data.Tags)
                {
                    rgData.Tags.Add(tag.Key, tag.Value);
                }
                rgData.Tags[TagAutoDeleteAfter] = newExpiry.ToString("o");
                rgData.Tags[TagSessionId] = $"pool-{poolName}-extended";

                await subscription.GetResourceGroups()
                    .CreateOrUpdateAsync(WaitUntil.Completed, expectedRgName, rgData, cancellationToken);

                Log.Write($"Extended pool '{poolName}' TTL to {newExpiry:u}.");
            }
            catch (Exception ex)
            {
                Log.WriteWarning($"Failed to extend pool '{poolName}' TTL: {ex.Message}");
            }
        }

        public async ValueTask DisposeAsync()
        {
            // Teardown all tracked sessions
            foreach (var sessionId in _sessionResourceGroups.Keys.ToList())
            {
                await TeardownAsync(sessionId);
            }

            _httpClient?.Dispose();
        }

        private async Task<SubscriptionResource> GetSubscriptionAsync(CancellationToken cancellationToken)
        {
            if (_subscription != null)
            {
                return _subscription;
            }

            return await _armClient.GetDefaultSubscriptionAsync(cancellationToken);
        }

        private static async Task<(VirtualNetworkResource vnet, NetworkSecurityGroupResource nsg)> CreateNetworkingAsync(
            ResourceGroupResource resourceGroup,
            string region,
            IDictionary<string, ProvisioningConfig> agentConfigs,
            CancellationToken cancellationToken)
        {
            var location = new AzureLocation(region);

            // Collect all agent ports used
            var agentPorts = agentConfigs.Values.Select(c => c.AgentPort).Distinct().ToList();

            // Create NSG with rules for agent ports
            var nsgData = new NetworkSecurityGroupData { Location = location };

            int priority = 100;
            foreach (var port in agentPorts)
            {
                nsgData.SecurityRules.Add(new SecurityRuleData
                {
                    Name = $"allow-crank-agent-{port}",
                    Priority = priority++,
                    Direction = SecurityRuleDirection.Inbound,
                    Access = SecurityRuleAccess.Allow,
                    Protocol = SecurityRuleProtocol.Tcp,
                    SourceAddressPrefix = "*",
                    SourcePortRange = "*",
                    DestinationAddressPrefix = "*",
                    DestinationPortRange = port.ToString()
                });
            }

            // Allow SSH for debugging
            nsgData.SecurityRules.Add(new SecurityRuleData
            {
                Name = "allow-ssh",
                Priority = priority++,
                Direction = SecurityRuleDirection.Inbound,
                Access = SecurityRuleAccess.Allow,
                Protocol = SecurityRuleProtocol.Tcp,
                SourceAddressPrefix = "*",
                SourcePortRange = "*",
                DestinationAddressPrefix = "*",
                DestinationPortRange = "22"
            });

            var nsgCollection = resourceGroup.GetNetworkSecurityGroups();
            var nsgOperation = await nsgCollection.CreateOrUpdateAsync(
                WaitUntil.Completed, "nsg-crank-agents", nsgData, cancellationToken);
            var nsg = nsgOperation.Value;

            // Create VNet + Subnet
            var vnetData = new VirtualNetworkData
            {
                Location = location,
                AddressPrefixes = { "10.0.0.0/16" },
                Subnets =
                {
                    new SubnetData
                    {
                        Name = "subnet-agents",
                        AddressPrefix = "10.0.1.0/24",
                        NetworkSecurityGroup = new NetworkSecurityGroupData { Id = nsg.Id }
                    }
                }
            };

            var vnetCollection = resourceGroup.GetVirtualNetworks();
            var vnetOperation = await vnetCollection.CreateOrUpdateAsync(
                WaitUntil.Completed, "vnet-crank", vnetData, cancellationToken);
            var vnet = vnetOperation.Value;

            return (vnet, nsg);
        }

        private static async Task<List<ProvisionedAgent>> ProvisionServiceVmsAsync(
            ResourceGroupResource resourceGroup,
            VirtualNetworkResource vnet,
            NetworkSecurityGroupResource nsg,
            string serviceName,
            ProvisioningConfig config,
            string region,
            string sessionId,
            CancellationToken cancellationToken)
        {
            var agents = new List<ProvisionedAgent>();
            var location = new AzureLocation(region);
            var subnetId = vnet.Data.Subnets.First().Id;

            var vmTasks = new List<Task<ProvisionedAgent>>();

            for (int i = 0; i < config.Count; i++)
            {
                var index = i;
                vmTasks.Add(ProvisionSingleVmAsync(
                    resourceGroup, subnetId, serviceName, config, location, sessionId, index, cancellationToken));
            }

            var results = await Task.WhenAll(vmTasks);
            agents.AddRange(results);

            return agents;
        }

        private static async Task<ProvisionedAgent> ProvisionSingleVmAsync(
            ResourceGroupResource resourceGroup,
            ResourceIdentifier subnetId,
            string serviceName,
            ProvisioningConfig config,
            AzureLocation location,
            string sessionId,
            int index,
            CancellationToken cancellationToken)
        {
            var vmName = $"vm-{serviceName}-{index}";
            var nicName = $"nic-{serviceName}-{index}";
            var pipName = $"pip-{serviceName}-{index}";

            Log.Write($"Provisioning VM '{vmName}' ({config.VmSize}, {config.Os})...");

            // Create Public IP
            var pipData = new PublicIPAddressData
            {
                Location = location,
                PublicIPAllocationMethod = NetworkIPAllocationMethod.Static,
                Sku = new PublicIPAddressSku { Name = PublicIPAddressSkuName.Standard }
            };

            var pipCollection = resourceGroup.GetPublicIPAddresses();
            var pipOperation = await pipCollection.CreateOrUpdateAsync(
                WaitUntil.Completed, pipName, pipData, cancellationToken);
            var pip = pipOperation.Value;

            // Create NIC
            var nicData = new NetworkInterfaceData
            {
                Location = location,
                IPConfigurations =
                {
                    new NetworkInterfaceIPConfigurationData
                    {
                        Name = "ipconfig1",
                        Subnet = new SubnetData { Id = subnetId },
                        PublicIPAddress = new PublicIPAddressData { Id = pip.Id }
                    }
                }
            };

            var nicCollection = resourceGroup.GetNetworkInterfaces();
            var nicOperation = await nicCollection.CreateOrUpdateAsync(
                WaitUntil.Completed, nicName, nicData, cancellationToken);
            var nic = nicOperation.Value;

            // Determine image reference
            var imageReference = GetImageReference(config);

            // Build cloud-init script
            var cloudInit = GenerateCloudInitScript(config);
            var cloudInitBase64 = Convert.ToBase64String(Encoding.UTF8.GetBytes(cloudInit));

            // Create VM
            var vmData = new VirtualMachineData(location)
            {
                HardwareProfile = new VirtualMachineHardwareProfile
                {
                    VmSize = new VirtualMachineSizeType(config.VmSize)
                },
                OSProfile = new VirtualMachineOSProfile
                {
                    ComputerName = vmName.Length > 15 ? vmName.Substring(0, 15) : vmName,
                    AdminUsername = "crankadmin",
                    CustomData = cloudInitBase64,
                    LinuxConfiguration = config.Os.ToLowerInvariant() == "linux"
                        ? new LinuxConfiguration
                        {
                            DisablePasswordAuthentication = true,
                            SshPublicKeys =
                            {
                                // Use a generated key pair — in production this would come from config
                                new SshPublicKeyConfiguration
                                {
                                    Path = "/home/crankadmin/.ssh/authorized_keys",
                                    KeyData = GenerateTemporarySshKey()
                                }
                            }
                        }
                        : null,
                    WindowsConfiguration = config.Os.ToLowerInvariant() == "windows"
                        ? new WindowsConfiguration
                        {
                            ProvisionVmAgent = true
                        }
                        : null
                },
                StorageProfile = new VirtualMachineStorageProfile
                {
                    ImageReference = imageReference,
                    OSDisk = new VirtualMachineOSDisk(DiskCreateOptionType.FromImage)
                    {
                        Name = $"osdisk-{serviceName}-{index}",
                        ManagedDisk = new VirtualMachineManagedDisk
                        {
                            StorageAccountType = StorageAccountType.PremiumLrs
                        },
                        DiskSizeGB = 128,
                        DeleteOption = DiskDeleteOptionType.Delete
                    }
                },
                NetworkProfile = new VirtualMachineNetworkProfile
                {
                    NetworkInterfaces =
                    {
                        new VirtualMachineNetworkInterfaceReference
                        {
                            Id = nic.Id,
                            Primary = true
                        }
                    }
                }
            };

            // Apply session tags
            vmData.Tags.Add(TagSessionId, sessionId);
            vmData.Tags.Add(TagCreatedAt, DateTime.UtcNow.ToString("o"));
            vmData.Tags.Add(TagManagedBy, "crank-controller");
            vmData.Tags.Add("crank-service", serviceName);

            // Apply custom tags
            foreach (var tag in config.Tags)
            {
                vmData.Tags[tag.Key] = tag.Value;
            }

            // Configure spot instance if requested
            if (config.SpotInstance)
            {
                vmData.Priority = VirtualMachinePriorityType.Spot;
                vmData.EvictionPolicy = VirtualMachineEvictionPolicyType.Deallocate;
                vmData.BillingMaxPrice = -1; // Pay up to on-demand price
            }

            var vmCollection = resourceGroup.GetVirtualMachines();
            var vmOperation = await vmCollection.CreateOrUpdateAsync(
                WaitUntil.Completed, vmName, vmData, cancellationToken);
            var vm = vmOperation.Value;

            // Get the public IP address
            var updatedPip = await pipCollection.GetAsync(pipName, cancellationToken: cancellationToken);
            var publicIp = updatedPip.Value.Data.IPAddress;

            Log.Write($"VM '{vmName}' provisioned with IP {publicIp}.");

            return new ProvisionedAgent
            {
                EndpointUri = new Uri($"http://{publicIp}:{config.AgentPort}"),
                IpAddress = publicIp,
                Hostname = vmName,
                ResourceGroupName = resourceGroup.Data.Name,
                VmName = vmName,
                ServiceName = serviceName
            };
        }

        private static ImageReference GetImageReference(ProvisioningConfig config)
        {
            if (!string.IsNullOrEmpty(config.CustomImageId))
            {
                return new ImageReference { Id = new ResourceIdentifier(config.CustomImageId) };
            }

            return config.Image?.ToLowerInvariant() switch
            {
                "ubuntu-22.04" => new ImageReference
                {
                    Publisher = "Canonical",
                    Offer = "0001-com-ubuntu-server-jammy",
                    Sku = "22_04-lts-gen2",
                    Version = "latest"
                },
                "ubuntu-24.04" => new ImageReference
                {
                    Publisher = "Canonical",
                    Offer = "ubuntu-24_04-lts",
                    Sku = "server",
                    Version = "latest"
                },
                "windows-2022" => new ImageReference
                {
                    Publisher = "MicrosoftWindowsServer",
                    Offer = "WindowsServer",
                    Sku = "2022-datacenter-g2",
                    Version = "latest"
                },
                "windows-2025" => new ImageReference
                {
                    Publisher = "MicrosoftWindowsServer",
                    Offer = "WindowsServer",
                    Sku = "2025-datacenter-g2",
                    Version = "latest"
                },
                _ => new ImageReference
                {
                    Publisher = "Canonical",
                    Offer = "0001-com-ubuntu-server-jammy",
                    Sku = "22_04-lts-gen2",
                    Version = "latest"
                }
            };
        }

        internal static string GenerateCloudInitScript(ProvisioningConfig config)
        {
            var sb = new StringBuilder();

            if (config.Os.ToLowerInvariant() == "linux")
            {
                sb.AppendLine("#!/bin/bash");
                sb.AppendLine("set -euo pipefail");
                sb.AppendLine();
                sb.AppendLine("# Log all output for diagnostics");
                sb.AppendLine("exec > /var/log/crank-agent-setup.log 2>&1");
                sb.AppendLine();

                if (!string.IsNullOrEmpty(config.AgentImage))
                {
                    // Docker-based agent deployment
                    sb.AppendLine("# Install Docker");
                    sb.AppendLine("curl -fsSL https://get.docker.com | sh");
                    sb.AppendLine("systemctl enable docker && systemctl start docker");
                    sb.AppendLine();
                    sb.AppendLine("# Pull and run the crank agent container");
                    sb.AppendLine($"docker pull {config.AgentImage}");
                    sb.AppendLine($"docker run -d --restart=always --name crank-agent \\");
                    sb.AppendLine($"  --network host \\");
                    sb.AppendLine($"  -v /var/run/docker.sock:/var/run/docker.sock \\");
                    sb.AppendLine($"  {config.AgentImage} --url http://*:{config.AgentPort}");
                }
                else if (!string.IsNullOrEmpty(config.AgentSource))
                {
                    // Build agent from source
                    GenerateLinuxBuildFromSourceScript(sb, config);
                }
                else
                {
                    // Direct dotnet tool install
                    GenerateLinuxDotnetToolInstallScript(sb, config);
                }
            }
            else
            {
                // Windows cloud-init (CustomScriptExtension would be used instead typically)
                sb.AppendLine("# PowerShell setup script for Windows");
                sb.AppendLine("$ErrorActionPreference = 'Stop'");
                sb.AppendLine();
                sb.AppendLine("# Install .NET SDK");
                sb.AppendLine("Invoke-WebRequest -Uri https://dot.net/v1/dotnet-install.ps1 -OutFile dotnet-install.ps1");
                sb.AppendLine(".\\dotnet-install.ps1 -Channel 8.0");
                sb.AppendLine("$env:PATH = \"$env:USERPROFILE\\.dotnet;$env:USERPROFILE\\.dotnet\\tools;$env:PATH\"");
                sb.AppendLine();

                if (!string.IsNullOrEmpty(config.AgentSource))
                {
                    // Build agent from source on Windows
                    sb.AppendLine("# Clone and build crank-agent from source");
                    sb.AppendLine($"git clone {config.AgentSource} C:\\crank-source");
                    sb.AppendLine("Set-Location C:\\crank-source");
                    sb.AppendLine($"git checkout {config.AgentSourceBranch}");
                    sb.AppendLine($"dotnet publish {config.AgentSourceProject} -c Release -o C:\\crank-agent");
                    sb.AppendLine();
                    sb.AppendLine("# Start the agent");
                    sb.AppendLine($"Start-Process -NoNewWindow -FilePath dotnet -ArgumentList 'C:\\crank-agent\\Microsoft.Crank.Agent.dll --url http://*:{config.AgentPort}'");
                }
                else
                {
                    sb.AppendLine("# Install crank-agent");
                    sb.AppendLine("dotnet tool install -g Microsoft.Crank.Agent --version \"0.2.0-*\"");
                    sb.AppendLine();
                    sb.AppendLine("# Start the agent");
                    sb.AppendLine($"Start-Process -NoNewWindow -FilePath crank-agent -ArgumentList '--url http://*:{config.AgentPort}'");
                }
            }

            return sb.ToString();
        }

        private static void GenerateLinuxBuildFromSourceScript(StringBuilder sb, ProvisioningConfig config)
        {
            sb.AppendLine("# Install prerequisites");
            sb.AppendLine("apt-get update && apt-get install -y --no-install-recommends \\");
            sb.AppendLine("    git procps curl wget libgdiplus gnupg2 software-properties-common");
            sb.AppendLine();
            sb.AppendLine("# Install .NET SDK");
            sb.AppendLine("wget -q https://dot.net/v1/dotnet-install.sh -O /tmp/dotnet-install.sh");
            sb.AppendLine("chmod +x /tmp/dotnet-install.sh");
            sb.AppendLine("/tmp/dotnet-install.sh --channel 8.0 --install-dir /usr/share/dotnet");
            sb.AppendLine("ln -sf /usr/share/dotnet/dotnet /usr/bin/dotnet");
            sb.AppendLine("export DOTNET_ROOT=/usr/share/dotnet");
            sb.AppendLine("export PATH=\"$PATH:/usr/share/dotnet\"");
            sb.AppendLine();
            sb.AppendLine("# Clone and build crank-agent from source");
            sb.AppendLine($"git clone {config.AgentSource} /opt/crank-source");
            sb.AppendLine("cd /opt/crank-source");
            sb.AppendLine($"git checkout {config.AgentSourceBranch}");
            sb.AppendLine("echo \"Building crank-agent from source at commit $(git rev-parse HEAD)...\"");
            sb.AppendLine($"dotnet publish {config.AgentSourceProject} -c Release -o /opt/crank-agent");
            sb.AppendLine();
            sb.AppendLine("# Create a systemd service for the agent");
            sb.AppendLine("cat > /etc/systemd/system/crank-agent.service << 'EOF'");
            sb.AppendLine("[Unit]");
            sb.AppendLine("Description=Crank Benchmarking Agent (built from source)");
            sb.AppendLine("After=network.target");
            sb.AppendLine();
            sb.AppendLine("[Service]");
            sb.AppendLine("Type=simple");
            sb.AppendLine($"ExecStart=/usr/share/dotnet/dotnet /opt/crank-agent/Microsoft.Crank.Agent.dll --url http://*:{config.AgentPort}");
            sb.AppendLine("Restart=always");
            sb.AppendLine("RestartSec=5");
            sb.AppendLine("WorkingDirectory=/opt/crank-agent");
            sb.AppendLine("Environment=DOTNET_ROOT=/usr/share/dotnet");
            sb.AppendLine("Environment=PATH=/usr/local/sbin:/usr/local/bin:/usr/sbin:/usr/bin:/sbin:/bin:/usr/share/dotnet");
            sb.AppendLine();
            sb.AppendLine("[Install]");
            sb.AppendLine("WantedBy=multi-user.target");
            sb.AppendLine("EOF");
            sb.AppendLine();
            sb.AppendLine("systemctl daemon-reload");
            sb.AppendLine("systemctl enable crank-agent");
            sb.AppendLine("systemctl start crank-agent");
        }

        private static void GenerateLinuxDotnetToolInstallScript(StringBuilder sb, ProvisioningConfig config)
        {
            sb.AppendLine("# Install prerequisites");
            sb.AppendLine("apt-get update && apt-get install -y --no-install-recommends \\");
            sb.AppendLine("    git procps curl wget libgdiplus gnupg2 software-properties-common");
            sb.AppendLine();
            sb.AppendLine("# Install .NET SDK");
            sb.AppendLine("wget -q https://dot.net/v1/dotnet-install.sh -O /tmp/dotnet-install.sh");
            sb.AppendLine("chmod +x /tmp/dotnet-install.sh");
            sb.AppendLine("/tmp/dotnet-install.sh --channel 8.0 --install-dir /usr/share/dotnet");
            sb.AppendLine("ln -sf /usr/share/dotnet/dotnet /usr/bin/dotnet");
            sb.AppendLine("export DOTNET_ROOT=/usr/share/dotnet");
            sb.AppendLine("export PATH=\"$PATH:/usr/share/dotnet:/root/.dotnet/tools\"");
            sb.AppendLine();
            sb.AppendLine("# Install crank-agent");
            sb.AppendLine("dotnet tool install -g Microsoft.Crank.Agent --version \"0.2.0-*\"");
            sb.AppendLine();
            sb.AppendLine("# Create a systemd service for the agent");
            sb.AppendLine("cat > /etc/systemd/system/crank-agent.service << 'EOF'");
            sb.AppendLine("[Unit]");
            sb.AppendLine("Description=Crank Benchmarking Agent");
            sb.AppendLine("After=network.target");
            sb.AppendLine();
            sb.AppendLine("[Service]");
            sb.AppendLine("Type=simple");
            sb.AppendLine($"ExecStart=/root/.dotnet/tools/crank-agent --url http://*:{config.AgentPort}");
            sb.AppendLine("Restart=always");
            sb.AppendLine("RestartSec=5");
            sb.AppendLine("Environment=DOTNET_ROOT=/usr/share/dotnet");
            sb.AppendLine("Environment=PATH=/usr/local/sbin:/usr/local/bin:/usr/sbin:/usr/bin:/sbin:/bin:/usr/share/dotnet:/root/.dotnet/tools");
            sb.AppendLine();
            sb.AppendLine("[Install]");
            sb.AppendLine("WantedBy=multi-user.target");
            sb.AppendLine("EOF");
            sb.AppendLine();
            sb.AppendLine("systemctl daemon-reload");
            sb.AppendLine("systemctl enable crank-agent");
            sb.AppendLine("systemctl start crank-agent");
        }

        private static string GenerateTemporarySshKey()
        {
            // In a real implementation, this would generate or load an SSH key pair.
            // For now, we return a placeholder that would need to be replaced with an actual key.
            // The controller should accept an SSH key via CLI option or generate one per session.
            return "ssh-rsa AAAAB3NzaC1yc2EAAAADAQABAAABgQC-PLACEHOLDER crank-agent-temp-key";
        }
    }
}
