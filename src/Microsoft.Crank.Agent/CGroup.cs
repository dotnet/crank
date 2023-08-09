// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Crank.Models;

namespace Microsoft.Crank.Agent
{
    public abstract class CGroup
    {
        // https://docs.docker.com/config/containers/resource_constraints/
        public const double DefaultDockerCfsPeriod = 100000;

        public static string GetCGroupController(Job job)
        {
            // Create a unique cgroup controller per agent
            return $"benchmarks-{Environment.ProcessId}-{job.Id}";
        }

        public static async Task<CGroup> GetCGroupVersionAsync()
        {
            var statResult = await ProcessUtil.RunAsync("stat", "-fc %T /sys/fs/cgroup/", captureOutput: true, log: true);
            var isV2 = statResult.StandardOutput.Trim() == "cgroup2fs";

            return isV2 ? new CGroupV2() : new CGroupV1();
        }

        public abstract Task SetAsync(Job job);

        public abstract Task<string> GetCpuStatAsync(Job job);

        public virtual async Task DeleteAsync(Job job)
        {
            var controller = GetCGroupController(job);

            await ProcessUtil.RunAsync("cgdelete", $"cpu,memory,cpuset:{controller}", log: true, throwOnError: false);
        }

        public virtual async Task<(string executable, string commandLine)> CreateAsync(Job job)
        {
            var controller = GetCGroupController(job);

            var cgcreate = await ProcessUtil.RunAsync("cgcreate", $"-g memory,cpu,cpuset:{controller}", log: true);

            if (cgcreate.ExitCode > 0)
            {
                job.Error += "Could not create cgroup";
                return (null, null);
            }

            await SetAsync(job);

            return ("cgexec", $"-g memory,cpu,cpuset:{controller}");
        }
    }
}
