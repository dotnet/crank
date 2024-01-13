// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.Security;
using Windows.Win32.System.JobObjects;
using Windows.Win32.System.SystemInformation;
using Windows.Win32.System.Threading;

namespace Microsoft.Crank.Agent
{
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Interoperability", "CA1416:Validate platform compatibility", Justification = "<Pending>")]
    public class WindowsLimiter : IDisposable
    {
        private bool _hasJobObj = false;

        private readonly SafeFileHandle _processHandle;
        private readonly SafeHandle _jobHandle;

        public WindowsLimiter(Process process) 
        {
            _processHandle = CheckWin32Result(PInvoke.OpenProcess_SafeHandle(PROCESS_ACCESS_RIGHTS.PROCESS_ALL_ACCESS, false, (uint)process.Id));

            var securityAttributes = new SECURITY_ATTRIBUTES();
            securityAttributes.nLength = (uint)Marshal.SizeOf(securityAttributes);

            _jobHandle = CheckWin32Result(PInvoke.CreateJobObject(securityAttributes, $"job-{process.Id}"));
        }

        public unsafe void SetMemLimit(ulong memoryLimitInBytes)
        {
            if (memoryLimitInBytes == 0)
            {
                return;
            }

            var limitInfo = new JOBOBJECT_EXTENDED_LIMIT_INFORMATION();
            var size = (uint)Marshal.SizeOf(limitInfo);
            var length = 0u;

            CheckWin32Result(PInvoke.QueryInformationJobObject(_jobHandle, JOBOBJECTINFOCLASS.JobObjectExtendedLimitInformation, &limitInfo, size, &length));

            limitInfo.BasicLimitInformation.LimitFlags |= JOB_OBJECT_LIMIT.JOB_OBJECT_LIMIT_JOB_MEMORY;
            limitInfo.JobMemoryLimit = (nuint)memoryLimitInBytes;

            CheckWin32Result(PInvoke.SetInformationJobObject(_jobHandle, JOBOBJECTINFOCLASS.JobObjectExtendedLimitInformation, &limitInfo, size));
            _hasJobObj = true;
        }

        public unsafe void SetCpuLimits(double? cpuRatio = null, IList<int> cpuSet = null)
        {
            if (cpuRatio != null && cpuRatio > 0)
            {
                // Set CpuRate to a percentage times 100. For example, to let the job use 20% of the CPU, 
                // set CpuRate to 2,000.  0.2 -> 20(%) * 100
                var cpuRate = (uint)(cpuRatio * 100 * 100);

                // divide the rate accordingly
                if (cpuSet != null && cpuSet.Count > 0)
                {
                    cpuRate /= (uint)Environment.ProcessorCount / (uint)cpuSet.Count;
                }

                cpuRate = Math.Min(10000U, cpuRate);
                
                var limitInfo = new JOBOBJECT_CPU_RATE_CONTROL_INFORMATION
                {
                    ControlFlags = JOB_OBJECT_CPU_RATE_CONTROL.JOB_OBJECT_CPU_RATE_CONTROL_ENABLE |
                    JOB_OBJECT_CPU_RATE_CONTROL.JOB_OBJECT_CPU_RATE_CONTROL_HARD_CAP,
                    Anonymous = new JOBOBJECT_CPU_RATE_CONTROL_INFORMATION._Anonymous_e__Union { CpuRate = cpuRate }
                };

                var size = (uint)Marshal.SizeOf(limitInfo);
                CheckWin32Result(PInvoke.SetInformationJobObject(_jobHandle, JOBOBJECTINFOCLASS.JobObjectCpuRateControlInformation, &limitInfo, size));

                _hasJobObj = true;
            }

            if (cpuSet != null && cpuSet.Count > 0)
            {
                // Calculate the required groups for the cpuSet

                var cpuCount = Environment.ProcessorCount;
                var bufferSize = (uint)(Marshal.SizeOf(typeof(SYSTEM_CPU_SET_INFORMATION)) * cpuCount);

                var buffer = stackalloc SYSTEM_CPU_SET_INFORMATION[cpuCount];

                CheckWin32Result(PInvoke.GetSystemCpuSetInformation(buffer, bufferSize, out uint returnedLength, _processHandle));

                IntPtr pointer = (nint)buffer;

                var information = new SYSTEM_CPU_SET_INFORMATION[cpuCount];

                for (var i = 0; i < cpuCount; i++)
                {
                    information[i] = Marshal.PtrToStructure<SYSTEM_CPU_SET_INFORMATION>(pointer);
                    pointer += (nint)information[i].Size;
                }

                var cpuSetGroups = information
                    .Where(i => cpuSet.Contains(i.Anonymous.CpuSet.LogicalProcessorIndex))
                    .GroupBy(x => x.Anonymous.CpuSet.Group, x => x.Anonymous.CpuSet)
                    .ToDictionary(x => x.Key)
                    ;

                var groupsBuffer = stackalloc GROUP_AFFINITY[cpuSetGroups.Count];
                var groupsBufferSize = Marshal.SizeOf(typeof(GROUP_AFFINITY)) * cpuSetGroups.Count;

                IntPtr groupsPtr = (nint)groupsBuffer;

                Log.Info("Setting Group Affinity...");

                foreach (var group in cpuSetGroups)
                {
                    var groupAffinity = new GROUP_AFFINITY
                    {
                        Group = group.Key,
                        Mask = (nuint)group.Value.Sum(x => Math.Pow(2, x.LogicalProcessorIndex))
                    };

                    Marshal.StructureToPtr(groupAffinity, groupsPtr, false);
                    groupsPtr += Marshal.SizeOf(typeof(GROUP_AFFINITY));

                    Log.Info($"GROUP: {groupAffinity.Group}, MASK: {Convert.ToString((int)groupAffinity.Mask, 2)}");
                }

                CheckWin32Result(PInvoke.SetInformationJobObject(_jobHandle, JOBOBJECTINFOCLASS.JobObjectGroupInformationEx, groupsBuffer, (uint)groupsBufferSize));

                _hasJobObj = true;
            }
        }

        public unsafe void Apply()
        {
            if (_hasJobObj)
            {
                CheckWin32Result(PInvoke.AssignProcessToJobObject(_jobHandle, _processHandle));
            }
        }

        public void Dispose()
        {
            _processHandle.Dispose();
            _jobHandle.Dispose();
        }

        private static T CheckWin32Result<T>(T result)
        {
            return result switch
            {
                SafeHandle handle when !handle.IsInvalid => result,
                HANDLE handle when (nint)WIN32_ERROR.ERROR_INVALID_HANDLE != handle.Value => result,
                uint n when n != 0xffffffff => result,
                bool b when b => result,
                BOOL b when b => result,
                WIN32_ERROR err when err == WIN32_ERROR.NO_ERROR => result,
                WIN32_ERROR err => throw new Win32Exception((int)err),
                NTSTATUS nt when nt.Value == 0 => result,
                NTSTATUS nt => throw new Win32Exception(nt.Value),
                _ => throw new Win32Exception()
            };
        }
    }
}
