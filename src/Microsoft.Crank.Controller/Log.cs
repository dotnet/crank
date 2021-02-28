﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Runtime.InteropServices;

namespace Microsoft.Crank.Controller
{
    public class Log
    {
        public static bool IsVerbose { get; set; }
        public static bool IsQuiet { get; set; }

        public static void Quiet(string message)
        {
            Console.WriteLine(message);
        }

        public static void WriteError(string message, bool notime = false)
        {
            Write(message, notime, ConsoleColor.Red);
        }

        public static void WriteWarning(string message, bool notime = false)
        {
            Write(message, notime, ConsoleColor.DarkYellow);
        }

        public static void Write(string message, bool notime = false, ConsoleColor color = ConsoleColor.White)
        {
            if (color != ConsoleColor.White)
            {
                Console.ForegroundColor = color;
            }

            if (!IsQuiet)
            {
                var time = DateTime.Now.ToString("hh:mm:ss.fff");
                if (notime)
                {
                    Console.WriteLine(message);
                }
                else
                {
                    Console.WriteLine($"[{time}] {message}");
                }
            }

            Console.ResetColor();
        }

        public static void Verbose(string message)
        {
            if (IsVerbose && !IsQuiet)
            {
                Write(message);
            }
        }

        public static void DisplayOutput(string content)
        {
            if (String.IsNullOrEmpty(content))
            {
                return;
            }

            #region Switching console mode on Windows to preserve colors for stdout

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                var iStdOut = GetStdHandle(STD_OUTPUT_HANDLE);
                if (GetConsoleMode(iStdOut, out uint outConsoleMode))
                {
                    var tempConsoleMode = outConsoleMode;

                    outConsoleMode |= ENABLE_VIRTUAL_TERMINAL_PROCESSING | DISABLE_NEWLINE_AUTO_RETURN;
                    if (!SetConsoleMode(iStdOut, outConsoleMode))
                    {
                        Console.WriteLine($"failed to set output console mode, error code: {GetLastError()}");
                    }

                    if (!SetConsoleMode(iStdOut, tempConsoleMode))
                    {
                        Console.WriteLine($"failed to restore console mode, error code: {GetLastError()}");
                    }
                }
            }

            #endregion

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                // Convert LF
                content = content?.Replace("\n", Environment.NewLine) ?? "";
            }

            Log.Write(content.Trim(), notime: true);
        }

        private const int STD_OUTPUT_HANDLE = -11;
        private const uint ENABLE_VIRTUAL_TERMINAL_PROCESSING = 0x0004;
        private const uint DISABLE_NEWLINE_AUTO_RETURN = 0x0008;

        [DllImport("kernel32.dll")]
        private static extern bool GetConsoleMode(IntPtr hConsoleHandle, out uint lpMode);

        [DllImport("kernel32.dll")]
        private static extern bool SetConsoleMode(IntPtr hConsoleHandle, uint dwMode);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern IntPtr GetStdHandle(int nStdHandle);

        [DllImport("kernel32.dll")]
        public static extern uint GetLastError();

    }
}
