// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;

namespace Microsoft.Crank.Agent
{
    public static class Log
    {
        public static void Info(string message, bool timestamp = true)
        {
            if (timestamp)
            {
                if (Startup.Logger == null)
                {
                    Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] {message}");
                }
                else
                {
                    Startup.Logger.Information(message);
                }
            }
            else
            {
                if (Startup.Logger == null)
                {
                    Console.WriteLine($"{message}");
                }
                else
                {
                    Startup.Logger.Information(message);
                }
            }
        }

        public static void Error(Exception e, string message = null)
        {
            if (Startup.Logger == null)
            {
                if (string.IsNullOrEmpty(message))
                {
                    Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] {e.Message}");
                }
                else
                {
                    Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] {message} {e.Message}");
                }
            }
            else
            {
                Startup.Logger.Error(e, message);
            }
        }
        public static void Error(string message)
        {
            if (Startup.Logger == null)
            {
                Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] {message}");
            }
            else
            {
                Startup.Logger.Error(message);
            }
        }

        public static void Warning(string message)
        {
            if (Startup.Logger == null)
            {
                Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] {message}");
            }
            else
            {
                Startup.Logger.Warning(message);
            }
        }

    }
}
