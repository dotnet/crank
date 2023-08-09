// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Crank.Models;

namespace Microsoft.Crank.Agent
{
    internal class CGroupV2 : CGroup
    {
        public override async Task Set(Job job)
        {
            var controller = GetCGroupController(job);

            if (job.MemoryLimitInBytes > 0)
            {
                await ProcessUtil.RunAsync("cgset", $"-r memory.max={job.MemoryLimitInBytes} {controller}", log: true);
            }
            else
            {
                await ProcessUtil.RunAsync("cgset", $"-r memory.max=max {controller}", log: true);
            }

            if (job.CpuLimitRatio > 0)
            {
                // $MAX $PERIOD
                // which indicates that the group may consume up to $MAX in each $PERIOD duration. "max" for $MAX indicates no limit. If only one number is written, $MAX is updated.
                await ProcessUtil.RunAsync("cgset", $"-r cpu.max=\"{Math.Floor(job.CpuLimitRatio * DefaultDockerCfsPeriod)} {DefaultDockerCfsPeriod}\" {controller}", log: true);
            }
            else
            {
                await ProcessUtil.RunAsync("cgset", $"-r cpu.max=\"max {DefaultDockerCfsPeriod}\" {controller}", log: true);
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
            var memsRoot = await File.ReadAllTextAsync($"/sys/fs/cgroup/{controller}/cpuset.mems");

            if (String.IsNullOrWhiteSpace(memsRoot))
            {
                memsRoot = await File.ReadAllTextAsync($"/sys/fs/cgroup/{controller}/cpuset.mems.effective");
            }

            // Both cpus and mems need to be initialized
            await ProcessUtil.RunAsync("cgset", $"-r cpuset.mems={memsRoot.Trim()} {controller}", log: true);
        }

        public override async Task<string> GetCpuStatAsync(Job job)
        {
            var controller = GetCGroupController(job);
            
            var result = await ProcessUtil.RunAsync("cat", $"/sys/fs/cgroup/{controller}/cpu.stat", throwOnError: false, captureOutput: true);
            return result.StandardOutput;
        }
    }
}
