// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Globalization;
using System.IO;
using System.Runtime.InteropServices;
using Xunit;

namespace Microsoft.Crank.IntegrationTests
{
    class CollectLinuxFactAttribute : FactAttribute
    {
        public CollectLinuxFactAttribute()
        {
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                Skip = "Test requires Linux";
            }
            else if (GetEffectiveUserId() != 0)
            {
                Skip = "Test requires root for perf_event_open";
            }
            else if (Environment.OSVersion.Version < new Version(6, 4))
            {
                Skip = "Test requires Linux kernel 6.4 or later";
            }
            else if (!HasPerfCapability())
            {
                Skip = "Test requires CAP_PERFMON or CAP_SYS_ADMIN";
            }
        }

        [DllImport("libc")]
        private static extern uint geteuid();

        private static uint GetEffectiveUserId() => geteuid();

        private static bool HasPerfCapability()
        {
            const int CapSysAdmin = 21;
            const int CapPerfmon = 38;

            foreach (string line in File.ReadLines("/proc/self/status"))
            {
                if (!line.StartsWith("CapEff:", StringComparison.Ordinal))
                {
                    continue;
                }

                ulong capabilities = ulong.Parse(line.AsSpan("CapEff:".Length), NumberStyles.HexNumber);
                return (capabilities & (1UL << CapSysAdmin)) != 0 ||
                    (capabilities & (1UL << CapPerfmon)) != 0;
            }

            return false;
        }
    }
}
