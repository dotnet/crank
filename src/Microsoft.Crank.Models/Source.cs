// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.IO;
using System.Text.RegularExpressions;

namespace Microsoft.Crank.Models
{
    public class Source
    {
        /// <summary>
        /// The name of a branch, or a commit hash starting with '#'
        /// </summary>
        public string BranchOrCommit { get; set; } = "";
        public string Repository { get; set; }
        public string Project { get; set; }
        public bool InitSubmodules { get; set; }
        public string DockerFile { get; set; }
        public string DockerPull { get; set; }
        public string DockerImageName { get; set; }
        public string DockerLoad { get; set; } // Relative to the docker folder
        public string DockerCommand { get; set; } // Optional command arguments for 'docker run'
        public string DockerContextDirectory { get; set; }
        public string DockerFetchPath { get; set; }
        public string LocalFolder { get; set; }

        /// <summary>
        /// When set by the controller, the server uses it to reuse the same source folder.
        /// The value should vary when the source does. When the server can't find the source folder, the LocalFolder property is cleared
        /// such that the controller doesn't send any local source.
        /// </summary>
        public string SourceKey { get; set;}

        /// <summary>
        /// When SourceKey is defined, indicates whether a build should still occur. 
        /// </summary>
        public bool NoBuild { get; set; }

        public bool IsDocker()
        {
            return !String.IsNullOrEmpty(DockerFile) || !String.IsNullOrEmpty(DockerImageName) || !String.IsNullOrEmpty(DockerPull);
        }

        public string GetNormalizedImageName()
        {
            if (!string.IsNullOrEmpty(DockerPull))
            {
                return DockerPull.ToLowerInvariant();
            }

            // If DockerLoad option is used, the image must be set to the one used to build it

            if (!string.IsNullOrEmpty(DockerLoad))
            {
                return DockerImageName;
            }

            if (!string.IsNullOrEmpty(DockerImageName))
            {
                // If the docker image name already starts with benchmarks, reuse it
                // This prefix is used to clean any dangling container that would not have been stopped automatically
                if (DockerImageName.StartsWith("benchmarks_"))
                {
                    return DockerImageName;
                }
                else
                {
                    return $"benchmarks_{DockerImageName}".ToLowerInvariant();
                }
            }
            else
            {
                return $"benchmarks_{Path.GetFileNameWithoutExtension(DockerFile)}".ToLowerInvariant();
            }            
        }

        // When set, contains the location of the uploaded source code
        public Attachment SourceCode { get; set; }
    }
}
