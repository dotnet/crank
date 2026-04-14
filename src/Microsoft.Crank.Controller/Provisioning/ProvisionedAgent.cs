// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;

namespace Microsoft.Crank.Controller.Provisioning
{
    /// <summary>
    /// Represents a dynamically provisioned agent endpoint.
    /// </summary>
    public class ProvisionedAgent
    {
        /// <summary>
        /// The URI of the provisioned crank agent (e.g., http://1.2.3.4:5010).
        /// </summary>
        public Uri EndpointUri { get; set; }

        /// <summary>
        /// The public IP address of the provisioned VM.
        /// </summary>
        public string IpAddress { get; set; }

        /// <summary>
        /// The hostname of the provisioned VM.
        /// </summary>
        public string Hostname { get; set; }

        /// <summary>
        /// The Azure Resource Group containing this VM.
        /// </summary>
        public string ResourceGroupName { get; set; }

        /// <summary>
        /// The name of the VM in Azure.
        /// </summary>
        public string VmName { get; set; }

        /// <summary>
        /// The service name this agent is provisioned for (e.g., "application", "load").
        /// </summary>
        public string ServiceName { get; set; }
    }
}
