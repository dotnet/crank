// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Microsoft.Crank.RegressionBot.Models
{
    public enum ChangeTypes
    {
        Diff,
        New,
        Removed
    }

    public class DependencyChange
    {
        /// <summary>
        /// e.g., "application", "load"
        /// </summary>
        public string Job { get; set; }

        /// <summary>
        /// e.g, "+kL3IPaqvdVHIVR8mUBvrw=="
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        /// e.g, "Microsoft.AspNetCore.App"
        /// </summary>
        public string[] Names { get; set; }

        /// <summary>
        /// e.g., "https://github.com/dotnet/runtime"
        /// </summary>
        public string RepositoryUrl { get; set; }

        /// <summary>
        /// e.g., "6.0.0-preview.5.21228.5"
        /// </summary>
        public string PreviousVersion { get; set; }

        /// <summary>
        /// e.g., "6.0.0-preview.5.21228.5"
        /// </summary>
        public string CurrentVersion { get; set; }

        /// <summary>
        /// e.g., "52c1d0b9b72f09fa7cf1f491d1c147dc173b7d60"
        /// </summary>
        public string PreviousCommitHash { get; set; }

        /// <summary>
        /// e.g., "52c1d0b9b72f09fa7cf1f491d1c147dc173b7d60"
        /// </summary>
        public string CurrentCommitHash { get; set; }

        public ChangeTypes ChangeType { get; set; }
    }
}
