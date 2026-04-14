// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;

namespace Microsoft.Crank.Controller.Provisioning
{
    /// <summary>
    /// Configuration for dynamically provisioning infrastructure for a crank agent.
    /// Specified in the profile YAML under the "provision" key for each agent.
    /// </summary>
    public class ProvisioningConfig
    {
        /// <summary>
        /// The cloud provider to use (e.g., "azure"). Currently only "azure" is supported.
        /// </summary>
        public string Provider { get; set; } = "azure";

        /// <summary>
        /// The VM size/SKU (e.g., "Standard_D4s_v5").
        /// </summary>
        public string VmSize { get; set; } = "Standard_D4s_v5";

        /// <summary>
        /// The operating system for the VM (e.g., "linux" or "windows").
        /// </summary>
        public string Os { get; set; } = "linux";

        /// <summary>
        /// The Azure region to deploy to (e.g., "eastus2").
        /// </summary>
        public string Region { get; set; } = "eastus2";

        /// <summary>
        /// The OS image to use. For Azure, this maps to a VM image URN.
        /// Examples: "ubuntu-22.04", "ubuntu-24.04", "windows-2022"
        /// </summary>
        public string Image { get; set; } = "ubuntu-22.04";

        /// <summary>
        /// A custom VM image resource ID to use instead of a marketplace image.
        /// When set, this takes precedence over <see cref="Image"/>.
        /// </summary>
        public string CustomImageId { get; set; }

        /// <summary>
        /// Number of VM instances to provision for this agent role.
        /// </summary>
        public int Count { get; set; } = 1;

        /// <summary>
        /// The port the crank agent will listen on.
        /// </summary>
        public int AgentPort { get; set; } = 5010;

        /// <summary>
        /// Optional Docker image containing the crank agent.
        /// When set, the VM will run the agent via Docker instead of dotnet tool install.
        /// </summary>
        public string AgentImage { get; set; }

        /// <summary>
        /// The Azure subscription ID to use. If not set, uses the default subscription.
        /// </summary>
        public string SubscriptionId { get; set; }

        /// <summary>
        /// Optional Azure Resource Group name to use. If not set, a unique name is generated.
        /// </summary>
        public string ResourceGroup { get; set; }

        /// <summary>
        /// Whether to use Azure Spot/Low-priority VMs for cost savings.
        /// </summary>
        public bool SpotInstance { get; set; }

        /// <summary>
        /// Additional tags to apply to the Azure resources.
        /// </summary>
        public Dictionary<string, string> Tags { get; set; } = new();
    }
}
