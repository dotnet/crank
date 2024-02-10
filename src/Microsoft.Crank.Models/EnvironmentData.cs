// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Runtime.InteropServices;

namespace Microsoft.Crank.Models
{
    /// <summary>
    /// A model that stores information about the environment of the current process.
    /// This is useful when defining conditions for pre or post commands on the controller.
    /// </summary>
    public class EnvironmentData
    {
        private static readonly string platform = GetCurrentPlatform();
        private static readonly string architecture = RuntimeInformation.OSArchitecture.ToString();

        public string Platform => platform;
        public string Architecture => architecture;
        
        private static string GetCurrentPlatform()
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                return "windows";
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                return "linux";
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                return "osx";
            }
            else
            {
                // Windows, Linux, and OSX are the only platforms that have predefined OSPlatform instances.
                return "other";
            }
        }
    }
}
