// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;

namespace Microsoft.Crank.Models
{
    public class Source
    {
        public const string DefaultSource = "default";

        /// <summary>
        /// The name of a branch, or a commit hash starting with '#'
        /// </summary>
        public string BranchOrCommit { get; set; } = "";
        public string Repository { get; set; }
        public bool InitSubmodules { get; set; }
        public string LocalFolder { get; set; }

        /// <summary>
        /// When set, will specify where the source data will be copied to on the agent.
        /// If not provided, will use the source name as the destination folder.
        /// </summary>
        public string DestinationFolder { get; set; }

        /// <summary>
        /// When true, the crank agent will cache the source so it doesn't need to be uploaded each time.
        /// </summary>
        public bool CacheOnAgent { get; set; } = true;

        /// <summary>
        /// When set by the controller, the server uses it to reuse the same source folder.
        /// The value should vary when the source does. When the server can't find the source folder, the LocalFolder property is cleared
        /// such that the controller doesn't send any local source.
        /// </summary>
        public string SourceKey { get; set; }

        // When set, contains the location of the uploaded source code
        public Attachment SourceCode { get; set; }

        [Obsolete("Now stored against the Job, rather than the Source")]
        public string Project { get; set; }
        [Obsolete("Now stored against the Job, rather than the Source")]
        public string DockerFile { get; set; }
        [Obsolete("Now stored against the Job, rather than the Source")]
        public string DockerPull { get; set; }
        [Obsolete("Now stored against the Job, rather than the Source")]
        public string DockerImageName { get; set; }
        [Obsolete("Now stored against the Job, rather than the Source")]
        public string DockerLoad { get; set; } // Relative to the docker folder
        [Obsolete("Now stored against the Job, rather than the Source")]
        public string DockerCommand { get; set; } // Optional command arguments for 'docker run'
        [Obsolete("Now stored against the Job, rather than the Source")]
        public string DockerContextDirectory { get; set; }
        [Obsolete("Now stored against the Job, rather than the Source")]
        public string DockerFetchPath { get; set; }
        [Obsolete("Now stored against the Job, rather than the Source")]
        public bool NoBuild { get; set; }

        public SourceKeyData GetSourceKeyData()
        {
            return new SourceKeyData
            {
                BranchOrCommit = BranchOrCommit,
                Repository = Repository,
                InitSubmodules = InitSubmodules,
                LocalFolder = LocalFolder
            };
        }
    }

    /// <summary>
    /// A class that stores all the properties that can be used as part of a cache key for the source.
    /// </summary>
    public class SourceKeyData
    {
        public string BranchOrCommit { get; set; }
        public string Repository { get; set; }
        public bool InitSubmodules { get; set; }
        public string LocalFolder { get; set; }
    }
}
