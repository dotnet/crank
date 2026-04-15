// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Crank.Controller.Provisioning
{
    /// <summary>
    /// Interface for infrastructure provisioners that can dynamically create and destroy
    /// crank agent environments in cloud providers.
    /// </summary>
    public interface IInfrastructureProvisioner : IAsyncDisposable
    {
        /// <summary>
        /// Provisions infrastructure for a set of agent roles defined by their provisioning configs.
        /// </summary>
        /// <param name="sessionId">A unique session identifier used for resource naming and tagging.</param>
        /// <param name="agentConfigs">
        /// A dictionary mapping service names (e.g., "application", "load") to their provisioning configurations.
        /// </param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>A list of provisioned agent endpoints ready to accept jobs.</returns>
        Task<IReadOnlyList<ProvisionedAgent>> ProvisionAsync(
            string sessionId,
            IDictionary<string, ProvisioningConfig> agentConfigs,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Tears down all infrastructure associated with a session.
        /// This should be called in a finally block to ensure cleanup even on failure.
        /// </summary>
        /// <param name="sessionId">The session ID used during provisioning.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        Task TeardownAsync(string sessionId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Waits for all provisioned agents to become healthy and ready to accept jobs.
        /// </summary>
        /// <param name="agents">The provisioned agents to check.</param>
        /// <param name="timeout">Maximum time to wait for agents to become ready.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>True if all agents are healthy; false if any agent failed to become ready.</returns>
        Task<bool> WaitForAgentsReadyAsync(
            IReadOnlyList<ProvisionedAgent> agents,
            TimeSpan timeout,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Finds and cleans up orphaned resource groups from previous failed sessions.
        /// </summary>
        /// <param name="maxAge">Maximum age of resource groups to consider orphaned.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The number of orphaned resource groups cleaned up.</returns>
        Task<int> CleanupOrphanedResourcesAsync(
            TimeSpan maxAge,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Searches for an existing provisioned pool by name that has healthy agents.
        /// Returns the provisioned agents if found, or null if no matching pool exists.
        /// </summary>
        /// <param name="poolName">The pool name to search for.</param>
        /// <param name="agentConfigs">The expected agent configurations to validate against.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>A list of provisioned agents from the existing pool, or null if not found.</returns>
        Task<IReadOnlyList<ProvisionedAgent>> FindExistingPoolAsync(
            string poolName,
            IDictionary<string, ProvisioningConfig> agentConfigs,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Extends the time-to-live of a provisioned pool so it remains available for subsequent runs.
        /// </summary>
        /// <param name="poolName">The pool name to extend.</param>
        /// <param name="ttl">How much additional time to keep the pool alive.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        Task ExtendPoolTtlAsync(
            string poolName,
            TimeSpan ttl,
            CancellationToken cancellationToken = default);
    }
}
