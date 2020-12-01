// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;

namespace Microsoft.Crank.Agent
{
    public static class Log
    {
        public static void WriteLine(string message, bool timestamp = true)
        {
            var time = DateTime.Now.ToString("hh:mm:ss.fff");
            if (timestamp)
            {
                Console.WriteLine($"[{time}] {message}");
            }
            else
            {
                Console.WriteLine($"{message}");
            }
        }

        public static void Write(string message, bool timestamp = true)
        {
            var time = DateTime.Now.ToString("hh:mm:ss.fff");
            if (timestamp)
            {
                Console.Write($"[{time}] {message}");
            }
            else
            {
                Console.Write($"{message}");
            }
        }
    }
}
