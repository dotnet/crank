// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.Diagnostics.NETCore.Client;
using Microsoft.Diagnostics.Tracing.Parsers;
using System;
using System.Collections.Generic;
using System.Diagnostics.Tracing;
using System.Linq;
using System.Text.RegularExpressions;

namespace Microsoft.Diagnostics.Tools.Trace
{

    internal static class TraceExtensions
    {
        public static string CLREventProviderName = "Microsoft-Windows-DotNETRuntime";

        private static EventLevel defaultEventLevel = EventLevel.Verbose;
        // Keep this in sync with runtime repo's clretwall.man and
        // dotnet/diagnostics src/Tools/dotnet-trace/ProviderUtils.cs.
        // Legacy/typo'd names are preserved alongside their modern
        // equivalents so existing crank configurations continue to work
        // byte-for-byte (PR A is strictly additive — see plan.md).
        private static Dictionary<string, long> CLREventKeywords = new Dictionary<string, long>(StringComparer.InvariantCultureIgnoreCase)
        {
            { "gc", 0x1 },
            { "gchandle", 0x2 },
            { "fusion", 0x4 },                                  // legacy name
            { "assemblyloader", 0x4 },                          // modern alias for "fusion"
            { "loader", 0x8 },
            { "jit", 0x10 },
            { "ngen", 0x20 },
            { "startenumeration", 0x40 },
            { "endenumeration", 0x80 },
            { "security", 0x400 },
            { "appdomainresourcemanagement", 0x800 },
            { "jittracing", 0x1000 },
            { "interop", 0x2000 },
            { "contention", 0x4000 },
            { "exception", 0x8000 },
            { "threading", 0x10000 },
            { "jittedmethodiltonativemap", 0x20000 },
            { "overrideandsuppressngenevents", 0x40000 },
            { "type", 0x80000 },
            { "gcheapdump", 0x100000 },
            { "gcsampledobjectallcationhigh", 0x200000 },       // legacy typo'd name
            { "gcsampledobjectallocationhigh", 0x200000 },      // corrected modern name
            { "gcheapsurvivalandmovement", 0x400000 },
            { "gcheapcollect", 0x800000 },                      // legacy name
            { "managedheapcollect", 0x800000 },                 // modern alias
            { "managedheadcollect", 0x800000 },                 // upstream-ProviderUtils alias
            { "gcheapandtypenames", 0x1000000 },
            { "gcsampledobjectallcationlow", 0x2000000 },       // legacy typo'd name
            { "gcsampledobjectallocationlow", 0x2000000 },      // corrected modern name
            { "perftrack", 0x20000000 },
            { "stack", 0x40000000 },
            { "threadtransfer", 0x80000000 },
            { "debugger", 0x100000000 },
            { "monitoring", 0x200000000 },
            { "codesymbols", 0x400000000 },
            { "eventsource", 0x800000000 },
            { "compilation", 0x1000000000 },
            { "compilationdiagnostic", 0x2000000000 },
            { "methoddiagnostic", 0x4000000000 },
            { "typediagnostic", 0x8000000000 },
            { "jitinstrumentationdata", 0x10000000000 },
            { "profiler", 0x20000000000 },
            { "waithandle", 0x40000000000 },
            { "allocationsampling", 0x80000000000 },
        };

        public static IEnumerable<EventPipeProvider> ToCLREventPipeProviders(string clreventslist)
        {
            if (String.IsNullOrEmpty(clreventslist))
            {
                return Enumerable.Empty<EventPipeProvider>();
            }

            var clrevents = clreventslist.Split("+");
            long clrEventsKeywordsMask = 0;
            for (var i = 0; i < clrevents.Length; i++)
            {
                if (CLREventKeywords.TryGetValue(clrevents[i], out var keyword))
                {
                    clrEventsKeywordsMask |= keyword;
                }
            }

            if (clrEventsKeywordsMask == 0)
            {
                return Enumerable.Empty<EventPipeProvider>();
            }

            return new [] { new EventPipeProvider(CLREventProviderName, defaultEventLevel, clrEventsKeywordsMask, null) };
        }

        // Returns true when `expression` is a non-empty '+'-joined list of recognized
        // CLR keyword names (case-insensitive). Used to classify provider tokens for
        // the `dotnet-trace` CLI, where unknown keywords passed via `--clrevents`
        // are rejected. Differs from `ToCLREventPipeProviders`, which silently drops
        // unknown parts and only requires *one* recognized keyword.
        internal static bool IsRecognizedClrKeywordExpression(string expression)
        {
            if (String.IsNullOrEmpty(expression))
            {
                return false;
            }

            var parts = expression.Split('+', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length == 0)
            {
                return false;
            }

            foreach (var part in parts)
            {
                if (!CLREventKeywords.ContainsKey(part))
                {
                    return false;
                }
            }

            return true;
        }

        private static EventLevel GetEventLevel(string token)
        {
            if (Int32.TryParse(token, out int level) && level >= 0)
            {
                return level > (int)EventLevel.Verbose ? EventLevel.Verbose : (EventLevel)level;
            }

            else
            {
                switch (token.ToLower())
                {
                    case "critical":
                        return EventLevel.Critical;
                    case "error":
                        return EventLevel.Error;
                    case "informational":
                        return EventLevel.Informational;
                    case "logalways":
                        return EventLevel.LogAlways;
                    case "verbose":
                        return EventLevel.Verbose;
                    case "warning":
                        return EventLevel.Warning;
                    default:
                        throw new ArgumentException($"Unknown EventLevel: {token}");
                }
            }
        }

        internal static IEnumerable<EventPipeProvider> ToProvider(string provider)
        {
            if (String.IsNullOrEmpty(provider))
            {
                return Enumerable.Empty<EventPipeProvider>();
            }

            var tokens = provider.Split(new[] { ':' }, 4, StringSplitOptions.None); // Keep empty tokens;

            // Provider name
            string providerName = tokens.Length > 0 ? tokens[0] : null;

            // Check if the supplied provider is a GUID and not a name.
            if (Guid.TryParse(providerName, out _))
            {
                return Enumerable.Empty<EventPipeProvider>();
            }

            if (string.IsNullOrWhiteSpace(providerName))
            {
                return Enumerable.Empty<EventPipeProvider>();
            }

            // Keywords
            long keywords = tokens.Length > 1 && !string.IsNullOrWhiteSpace(tokens[1]) ?
                Convert.ToInt64(tokens[1], 16) : -1;

            // Level
            EventLevel eventLevel = tokens.Length > 2 && !string.IsNullOrWhiteSpace(tokens[2]) ?
                GetEventLevel(tokens[2]) : defaultEventLevel;

            // Event counters
            string filterData = tokens.Length > 3 ? tokens[3] : null;
            var argument = string.IsNullOrWhiteSpace(filterData) ? null : ParseArgumentString(filterData); 
            return new [] { new EventPipeProvider(providerName, eventLevel, keywords, argument) };
        }

        private static Dictionary<string, string> ParseArgumentString(string argument)
        {
            if (argument == "")
            {
                return null;
            }
            var argumentDict = new Dictionary<string, string>();

            int keyStart = 0;
            int keyEnd = 0;
            int valStart = 0;
            int valEnd = 0;
            int curIdx = 0;
            bool inQuote = false;
            argument = Regex.Unescape(argument);
            foreach (var c in argument)
            {
                if (inQuote)
                {
                    if (c == '\"')
                    {
                        inQuote = false;
                    }
                }
                else
                {
                    if (c == '=')
                    {
                        keyEnd = curIdx;
                        valStart = curIdx+1;
                    }
                    else if (c == ';')
                    {
                        valEnd = curIdx;
                        AddKeyValueToArgumentDict(argumentDict, argument, keyStart, keyEnd, valStart, valEnd);
                        keyStart = curIdx+1; // new key starts
                    }
                    else if (c == '\"')
                    {
                        inQuote = true;
                    }
                }
                curIdx += 1;
            }
            AddKeyValueToArgumentDict(argumentDict, argument, keyStart, keyEnd, valStart, valEnd);
            return argumentDict;
        }

        private static void AddKeyValueToArgumentDict(Dictionary<string, string> argumentDict, string argument, int keyStart, int keyEnd, int valStart, int valEnd)
        {
            string key = argument.Substring(keyStart, keyEnd - keyStart);
            string val = argument.Substring(valStart);
            if (val.StartsWith("\"") && val.EndsWith("\""))
            {
                val = val.Substring(1, val.Length - 2);
            }
            argumentDict.Add(key, val);
        }

        internal static IEnumerable<EventPipeProvider> Merge(IEnumerable<EventPipeProvider> collection1, IEnumerable<EventPipeProvider> collection2)
        {
            var result = collection1.ToDictionary(x => x.Name, x => x, StringComparer.OrdinalIgnoreCase);

            foreach (var provider in collection2)
            {
                result[provider.Name] = provider;
            }

            return result.Values.ToArray();
        } 

        internal static Dictionary<string, EventPipeProvider[]> DotNETRuntimeProfiles { get; } = new Dictionary<string, EventPipeProvider[]>(StringComparer.OrdinalIgnoreCase) {
            // ---- Legacy crank profiles preserved for back-compat ----
            // The original `cpu-sampling` profile here predates upstream's
            // 2024 reorganization of dotnet-trace profiles. Upstream now
            // splits the work into `dotnet-sampled-thread-time`
            // (SampleProfiler only) and `dotnet-common` (DotNETRuntime
            // with broader keywords). Keep this definition unchanged so
            // existing crank pipelines passing `--dotNetTraceProviders
            // cpu-sampling` get the same trace contents as before.
            {
                "cpu-sampling",
                new EventPipeProvider[] {
                    new EventPipeProvider(
                        name: "Microsoft-DotNETCore-SampleProfiler",
                        eventLevel: EventLevel.Informational
                    ),
                    new EventPipeProvider(
                        name: "Microsoft-Windows-DotNETRuntime",
                        keywords: (long) ClrTraceEventParser.Keywords.Default,
                        eventLevel: EventLevel.Informational
                    ),
                }
            },
            {
                "gc-verbose",
                new EventPipeProvider[] {
                    new EventPipeProvider(
                        name: "Microsoft-Windows-DotNETRuntime",
                        keywords: (long)ClrTraceEventParser.Keywords.GC |
                                  (long)ClrTraceEventParser.Keywords.GCHandle |
                                  (long)ClrTraceEventParser.Keywords.Exception,
                        eventLevel: EventLevel.Verbose
                    ),
                }
            },
            {
                "gc-collect",
                new EventPipeProvider[] {
                    new EventPipeProvider(
                        name: "Microsoft-Windows-DotNETRuntime",
                        keywords:   (long)ClrTraceEventParser.Keywords.GC |
                                    (long)ClrTraceEventParser.Keywords.Exception,
                        eventLevel: EventLevel.Informational
                    ),
                }
            },

            // ---- Modern profiles imported from dotnet/diagnostics ----
            // Mirrors ListProfilesCommandHandler.TraceProfiles. Lets
            // users opt into the upstream-recommended defaults without
            // changing the legacy `cpu-sampling` behavior.
            {
                "dotnet-common",
                new EventPipeProvider[] {
                    new EventPipeProvider(
                        name: "Microsoft-Windows-DotNETRuntime",
                        // 0x1 GC | 0x4 AssemblyLoader | 0x8 Loader | 0x10 JIT |
                        // 0x8000 Exceptions | 0x10000 Threading | 0x20000 JittedMethodILToNativeMap |
                        // 0x1000000000 Compilation
                        keywords: 0x100003801DL,
                        eventLevel: EventLevel.Informational
                    ),
                }
            },
            {
                "dotnet-sampled-thread-time",
                new EventPipeProvider[] {
                    new EventPipeProvider(
                        name: "Microsoft-DotNETCore-SampleProfiler",
                        eventLevel: EventLevel.Informational
                    ),
                }
            },
            {
                // Friendly alias matching the provider name; identical to
                // dotnet-sampled-thread-time.
                "sample-profiler",
                new EventPipeProvider[] {
                    new EventPipeProvider(
                        name: "Microsoft-DotNETCore-SampleProfiler",
                        eventLevel: EventLevel.Informational
                    ),
                }
            },
            {
                "database",
                new EventPipeProvider[] {
                    new EventPipeProvider(
                        name: "System.Threading.Tasks.TplEventSource",
                        eventLevel: EventLevel.Informational,
                        // TplEtwProviderTraceEventParser.Keywords.TasksFlowActivityIds
                        keywords: 0x80
                    ),
                    new EventPipeProvider(
                        name: "Microsoft-Diagnostics-DiagnosticSource",
                        eventLevel: EventLevel.Verbose,
                        // DiagnosticSourceEventSource Messages | Events
                        keywords: 0x3,
                        arguments: new Dictionary<string, string> {
                            {
                                "FilterAndPayloadSpecs",
                                "SqlClientDiagnosticListener/System.Data.SqlClient.WriteCommandBefore@Activity1Start:-Command;Command.CommandText;ConnectionId;Operation;Command.Connection.ServerVersion;Command.CommandTimeout;Command.CommandType;Command.Connection.ConnectionString;Command.Connection.Database;Command.Connection.DataSource;Command.Connection.PacketSize\r\n" +
                                "SqlClientDiagnosticListener/System.Data.SqlClient.WriteCommandAfter@Activity1Stop:\r\n" +
                                "Microsoft.EntityFrameworkCore/Microsoft.EntityFrameworkCore.Database.Command.CommandExecuting@Activity2Start:-Command.CommandText;Command;ConnectionId;IsAsync;Command.Connection.ClientConnectionId;Command.Connection.ServerVersion;Command.CommandTimeout;Command.CommandType;Command.Connection.ConnectionString;Command.Connection.Database;Command.Connection.DataSource;Command.Connection.PacketSize\r\n" +
                                "Microsoft.EntityFrameworkCore/Microsoft.EntityFrameworkCore.Database.Command.CommandExecuted@Activity2Stop:"
                            }
                        }
                    ),
                }
            }
        };
    }
}
