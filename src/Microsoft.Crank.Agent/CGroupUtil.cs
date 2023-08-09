// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Crank.Models;

namespace Microsoft.Crank.Agent
{
    public static class CGroupUtil
    {
        // https://docs.docker.com/config/containers/resource_constraints/
        internal const double DefaultDockerCfsPeriod = 100000;

        internal static string GetCGroupController(Job job)
        {
            // Create a unique cgroup controller per agent
            return $"benchmarks-{Environment.ProcessId}-{job.Id}";
        }

        private static async Task<bool> IsCGroupV2()
        {
            var statResult = await ProcessUtil.RunAsync("stat", "-fc %T /sys/fs/cgroup/", captureOutput: true, log: true);
            return statResult.StandardOutput.Trim() == "cgroup2fs";
        }

        private static async Task DoCGroupV1(string controller, Job job)
        {
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

        private static async Task DoCGroupV2(string controller, Job job)
        {
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

        public static async Task<(string executable, string commandLine)> InitAndGetCGroupCmd(Job job)
        {
            var controller = GetCGroupController(job);

            var isV2 = await IsCGroupV2();

            var cgcreate = await ProcessUtil.RunAsync("cgcreate", $"-g memory,cpu,cpuset:{controller}", log: true);

            if (cgcreate.ExitCode > 0)
            {
                job.Error += "Could not create cgroup";
                return (null, null);
            }

            if (isV2)
            {
                await DoCGroupV2(controller, job);
            }
            else
            { 
                await DoCGroupV1(controller, job);
            }

            return ("cgexec", $"-g memory,cpu,cpuset:{controller}");
        }

        public static async Task Delete(string controller)
        {
            await ProcessUtil.RunAsync("cgdelete", $"cpu,memory,cpuset:{controller}", log: true, throwOnError: false);
        }

        public static async Task<string> GetCpuStat(string controller)
        {
            var isV2 = await IsCGroupV2();

            if (isV2)
            {
                var result = await ProcessUtil.RunAsync("cat", $"/sys/fs/cgroup/{controller}/cpu.stat", throwOnError: false, captureOutput: true);
                return result.StandardOutput;
            }
            else
            {
                var result = await ProcessUtil.RunAsync("cat", $"/sys/fs/cgroup/cpu/{controller}/cpu.stat", throwOnError: false, captureOutput: true);
                return result.StandardOutput;
            }
        }
    }
}
