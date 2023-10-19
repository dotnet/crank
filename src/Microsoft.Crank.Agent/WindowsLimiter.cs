// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using static Vanara.PInvoke.Kernel32;
using Vanara.PInvoke;
using Microsoft.Crank.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Microsoft.Crank.Agent
{
    public class WindowsLimiter : IDisposable
    {
        private bool _hasJobObj = false;

        private readonly SafeHPROCESS _safeProcess;
        private readonly SafeHJOB _safeJob;
        
        private readonly Job _job;
        private readonly List<int> _cpuSet;

        public WindowsLimiter(Job job, uint processId) 
        {
            _job = job;

            _safeProcess = OpenProcess(ACCESS_MASK.MAXIMUM_ALLOWED, false, processId);
            _safeJob = CreateJobObject(null, $"{job.RunId}-{job.Service}");

            if (!String.IsNullOrWhiteSpace(job.CpuSet))
            {
                _cpuSet = Startup.CalculateCpuList(job.CpuSet);
            }
        }

        private void SetMemLimit()
        {
            var bi = QueryInformationJobObject<JOBOBJECT_EXTENDED_LIMIT_INFORMATION>(_safeJob, JOBOBJECTINFOCLASS.JobObjectExtendedLimitInformation);
            bi.BasicLimitInformation.LimitFlags |= JOBOBJECT_LIMIT_FLAGS.JOB_OBJECT_LIMIT_JOB_MEMORY;
            bi.JobMemoryLimit = _job.MemoryLimitInBytes;

            SetInformationJobObject(_safeJob, JOBOBJECTINFOCLASS.JobObjectExtendedLimitInformation, bi);
            Log.Info($"Creating Job Object with memory limits: {_job.MemoryLimitInBytes / 1024 / 1024:n0} MB, ({_job.Service}:{_job.Id})");
            _hasJobObj = true;
        }

        private void SetCpuRatio()
        {
            // Set CpuRate to a percentage times 100. For example, to let the job use 20% of the CPU, 
            // set CpuRate to 2,000.  0.2 -> 20(%) * 100
            var cpuRate = (uint)(_job.CpuLimitRatio * 100 * 100);

            // divide the rate accordingly
            if (_cpuSet != null && _cpuSet.Count > 0)
            {
                cpuRate /= ((uint)Environment.ProcessorCount / (uint)_cpuSet.Count);
            }

            cpuRate = Math.Min(10000U, cpuRate);
            var info = new JOBOBJECT_CPU_RATE_CONTROL_INFORMATION
            {
                ControlFlags = JOB_OBJECT_CPU_RATE_CONTROL_FLAGS.JOB_OBJECT_CPU_RATE_CONTROL_ENABLE |
                                JOB_OBJECT_CPU_RATE_CONTROL_FLAGS.JOB_OBJECT_CPU_RATE_CONTROL_HARD_CAP,
                Union = new JOBOBJECT_CPU_RATE_CONTROL_INFORMATION.CPU_RATE_CONTROL_UNION
                {
                    CpuRate = cpuRate
                }
            };

            SetInformationJobObject(_safeJob, JOBOBJECTINFOCLASS.JobObjectCpuRateControlInformation, info);
            Log.Info($"Creating Job Object with cpurate limits: {cpuRate}, ({_job.Service}:{_job.Id})");
            _hasJobObj = true;
        }

        public void LimitProcess()
        {
            if (_job.MemoryLimitInBytes > 0)
            {
                SetMemLimit();
            }

            if (_job.CpuLimitRatio > 0)
            {
                SetCpuRatio();
            }

            if (_cpuSet != null && _cpuSet.Any())
            {
                var ssi = GetSystemCpuSetInformation(_safeProcess).ToArray();
                var cpuSets = _cpuSet.Select(i => ssi[i].CpuSet.Id).ToArray();

                var result = SetProcessDefaultCpuSets(_safeProcess, cpuSets, (uint)cpuSets.Length);
                Log.Info($"Limiting CpuSet ids: {String.Join(',', cpuSets)}, {result} ({_job.Service}:{_job.Id})");

                foreach (var csi in ssi)
                {
                    Log.Info($"Id: {csi.CpuSet.Id}; NumaNodeIndex: {csi.CpuSet.NumaNodeIndex}; LogicalProcessorIndex: {csi.CpuSet.LogicalProcessorIndex}; CoreIndex: {csi.CpuSet.CoreIndex}; Group: {csi.CpuSet.Group}");
                }
            }

            if (_hasJobObj)
            {
                var result = AssignProcessToJobObject(_safeJob, _safeProcess);
                Log.Info($"Assign Job Object {result}, ({_job.Service}:{_job.Id})");
            }
        }

        public void Dispose()
        {
            Log.Info($"Releasing job object ({_job.Service}:{_job.Id})");
            TerminateJobObject(_safeJob, 0);
            _safeProcess.Dispose();
        }
    }
}
