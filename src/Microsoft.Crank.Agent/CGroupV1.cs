// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Crank.Models;

namespace Microsoft.Crank.Agent
{
    internal class CGroupV1 : CGroup
    {
        public override async Task SetAsync(Job job)
        {
            var controller = GetCGroupController(job);

            if (job.MemoryLimitInBytes > 0)
            {
                await ProcessUtil.RunAsync("cgset", $"-r memory.limit_in_bytes={job.MemoryLimitInBytes} {controller}", log: true);
            }
            else
            {
                await ProcessUtil.RunAsync("cgset", $"-r memory.limit_in_bytes=-1 {controller}", log: true);
            }

            if (job.CpuLimitRatio > 0)
            {
                // Ensure the cfs_period_us is the same as what docker would use
                await ProcessUtil.RunAsync("cgset", $"-r cpu.cfs_period_us={DefaultDockerCfsPeriod} {controller}", log: true);
                await ProcessUtil.RunAsync("cgset", $"-r cpu.cfs_quota_us={Math.Floor(job.CpuLimitRatio * DefaultDockerCfsPeriod)} {controller}", log: true);
            }
            else
            {
                await ProcessUtil.RunAsync("cgset", $"-r cpu.cfs_quota_us=-1 {controller}", log: true);
            }

            if (!String.IsNullOrEmpty(job.CpuSet))
            {
                await ProcessUtil.RunAsync("cgset", $"-r cpuset.cpus={job.CpuSet} {controller}", log: true);
            }
            else
            {
                await ProcessUtil.RunAsync("cgset", $"-r cpuset.cpus=0-{Environment.ProcessorCount - 1} {controller}", log: true);
            }

            // The cpuset.mems value for the 'benchmarks' controller needs to match the root one
            // to be compatible with the allowed nodes
            var memsRoot = await File.ReadAllTextAsync("/sys/fs/cgroup/cpuset/cpuset.mems");

            // Both cpus and mems need to be initialized
            await ProcessUtil.RunAsync("cgset", $"-r cpuset.mems={memsRoot.Trim()} {controller}", log: true);
        }

        public override async Task<string> GetCpuStatAsync(Job job)
        {
            var controller = GetCGroupController(job);
            
            var result = await ProcessUtil.RunAsync("cat", $"/sys/fs/cgroup/cpu/{controller}/cpu.stat", throwOnError: false, captureOutput: true);
            return result.StandardOutput;
        }
    }
}
