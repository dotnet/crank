// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;

namespace Microsoft.Crank.Controller.Provisioning
{
    /// <summary>
    /// Parses provisioning configuration from profile YAML/JSON.
    /// Detects whether a profile's agents use static endpoints or dynamic provisioning.
    /// </summary>
    public static class ProvisioningConfigParser
    {
        /// <summary>
        /// Extracts provisioning configurations from a merged profile JObject.
        /// Returns a dictionary of service name → ProvisioningConfig for services that
        /// have a "provision" block instead of (or in addition to) static "endpoints".
        /// </summary>
        /// <param name="profile">The merged profile JObject.</param>
        /// <param name="scenarioServices">The service names in the current scenario.</param>
        /// <returns>
        /// Dictionary of service name → ProvisioningConfig. Empty if no provisioning is needed.
        /// </returns>
        public static Dictionary<string, ProvisioningConfig> ExtractProvisioningConfigs(
            JObject profile,
            IEnumerable<string> scenarioServices)
        {
            var result = new Dictionary<string, ProvisioningConfig>();

            var agents = (profile["agents"] ?? profile["jobs"]) as JObject;
            if (agents == null)
            {
                return result;
            }

            foreach (var serviceName in scenarioServices)
            {
                var agentConfig = FindAgentConfig(agents, serviceName);
                if (agentConfig == null)
                {
                    continue;
                }

                var provision = agentConfig["provision"] as JObject;
                if (provision == null)
                {
                    continue;
                }

                var config = new ProvisioningConfig
                {
                    Provider = provision["provider"]?.Value<string>() ?? "azure",
                    VmSize = provision["vmSize"]?.Value<string>() ?? "Standard_D4s_v5",
                    Os = provision["os"]?.Value<string>() ?? "linux",
                    Region = provision["region"]?.Value<string>() ?? "eastus2",
                    Image = provision["image"]?.Value<string>() ?? "ubuntu-22.04",
                    CustomImageId = provision["customImageId"]?.Value<string>(),
                    Count = provision["count"]?.Value<int>() ?? 1,
                    AgentPort = provision["agentPort"]?.Value<int>() ?? 5010,
                    AgentImage = provision["agentImage"]?.Value<string>(),
                    SubscriptionId = provision["subscriptionId"]?.Value<string>(),
                    ResourceGroup = provision["resourceGroup"]?.Value<string>(),
                    SpotInstance = provision["spotInstance"]?.Value<bool>() ?? false
                };

                // Parse additional tags
                var tags = provision["tags"] as JObject;
                if (tags != null)
                {
                    foreach (var tagProperty in tags.Properties())
                    {
                        config.Tags[tagProperty.Name] = tagProperty.Value.Value<string>();
                    }
                }

                result[serviceName] = config;
            }

            return result;
        }

        /// <summary>
        /// Checks whether a merged profile contains any provisioning configurations.
        /// </summary>
        public static bool HasProvisioningConfigs(JObject profile, IEnumerable<string> scenarioServices)
        {
            var agents = (profile["agents"] ?? profile["jobs"]) as JObject;
            if (agents == null)
            {
                return false;
            }

            return scenarioServices.Any(serviceName =>
            {
                var agentConfig = FindAgentConfig(agents, serviceName);
                return agentConfig?["provision"] != null;
            });
        }

        private static JObject FindAgentConfig(JObject agents, string serviceName)
        {
            // Direct name match
            if (agents[serviceName] is JObject direct)
            {
                return direct;
            }

            // Check aliases
            foreach (var prop in agents.Properties())
            {
                if (prop.Value is JObject v
                    && v.TryGetValue("aliases", out var aliases)
                    && aliases is JArray aliasesArray
                    && aliasesArray.Values<string>().Contains(serviceName))
                {
                    return v;
                }
            }

            return null;
        }
    }
}
