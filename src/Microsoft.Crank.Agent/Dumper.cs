// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using Microsoft.Crank.Models;
using Microsoft.Diagnostics.NETCore.Client;

namespace Microsoft.Crank.Agent
{
    public partial class Dumper
    {

        public Dumper()
        {
        }

        public int Collect(int processId, string outputFilePath, DumpTypeOption type)
        {
            try
            {
                // Make sure the dump path is NOT relative. This path could be sent to the runtime 
                // process on Linux which may have a different current directory.
                outputFilePath = Path.GetFullPath(outputFilePath);

                // Display the type of dump and dump path
                string dumpTypeMessage = null;
                switch (type)
                {
                    case DumpTypeOption.Full:
                        dumpTypeMessage = "full";
                        break;
                    case DumpTypeOption.Heap:
                        dumpTypeMessage = "dump with heap";
                        break;
                    case DumpTypeOption.Mini:
                        dumpTypeMessage = "dump";
                        break;
                }

                Log.WriteLine($"Writing {dumpTypeMessage} to {outputFilePath}");

                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    // Get the process
                    Process process = Process.GetProcessById(processId);

                    Windows.CollectDump(process, outputFilePath, type);
                }
                else
                {
                    var client = new DiagnosticsClient(processId);

                    DumpType dumpType = DumpType.Normal;
                    switch (type)
                    {
                        case DumpTypeOption.Full:
                            dumpType = DumpType.Full;
                            break;
                        case DumpTypeOption.Heap:
                            dumpType = DumpType.WithHeap;
                            break;
                        case DumpTypeOption.Mini:
                            dumpType = DumpType.Normal;
                            break;
                    }

                    // Send the command to the runtime to initiate the core dump
                    client.WriteDump(dumpType, outputFilePath, logDumpGeneration: false);
                }
            }
            catch (Exception ex) when
                (ex is FileNotFoundException ||
                 ex is DirectoryNotFoundException ||
                 ex is UnauthorizedAccessException ||
                 ex is PlatformNotSupportedException ||
                 ex is InvalidDataException ||
                 ex is InvalidOperationException ||
                 ex is NotSupportedException ||
                 ex is DiagnosticsClientException)
            {
                Log.WriteLine($"{ex.Message}");
                return 1;
            }

            Log.WriteLine($"Dump complete");
            return 0;
        }
    }
}
