// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics.Tracing;
using System.Reflection;

namespace Microsoft.Crank.EventSources
{
    internal sealed class BenchmarksEventSource : EventSource
    {
        public static readonly BenchmarksEventSource Log = new BenchmarksEventSource();

        internal BenchmarksEventSource()
            : this("Benchmarks")
        {
        }

        // Used for testing
        internal BenchmarksEventSource(string eventSourceName)
            : base(eventSourceName)
        {
        }

        /// <summary>
        /// Measures a <see cref="long"> value.
        /// </summary>
        public static void Measure(string name, long value)
        {
            Log.MeasureLong(name, value);
        }

        /// <summary>
        /// Measures a <see cref="double"> value.
        /// </summary>
        public static void Measure(string name, double value)
        {
            Log.MeasureDouble(name, value);
        }

        /// <summary>
        /// Measures a <see cref="string"> value.
        /// </summary>
        public static void Measure(string name, string value)
        {
            Log.MeasureString(name, value);
        }

        /// <summary>
        /// Instructs the host agent to track CPU and memory usage for the specified process id.
        /// </summary>
        public static void SetChildProcessId(int pid)
        {
            Console.Error.WriteLine($"##ChildProcessId:{pid}");
        }

        /// <summary>
        /// Registers a measure and its properties. This is called once per measure. 
        /// </summary>
        /// <param name="name">The name of the measure. Usually with the format 'source/property', e.g. 'wrk/rps'</param>
        /// <param name="aggregate">The operation to apply on the list of measures.</param>
        /// <param name="reduce">The operation to apply on a set of aggregates.</param>
        /// <param name="shortDescription">A short description to display in UI, including unit.</param>
        /// <param name="longDescription">A long description.</param>
        /// <param name="format">A .NET format string, e.g. n2, json.</param>
        public static void Register(string name, Operations aggregate, Operations reduce, string shortDescription, string longDescription, string format)
        {
            Log.Metadata(name, aggregate.ToString(), reduce.ToString(), shortDescription, longDescription, format);
        }        

        /// <summary>
        /// Adds the ASP.NET Core version as a measure.
        /// </summary>
        public static void MeasureAspNetVersion()
        {
            var iWebHostBuilderType = Type.GetType("Microsoft.AspNetCore.Hosting.IWebHostBuilder");

            if (iWebHostBuilderType != null)
            {
                var aspnetCoreVersion = iWebHostBuilderType.GetTypeInfo().Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;

                if (aspnetCoreVersion != null)
                {
                    Log.MeasureString("AspNetCoreVersion", aspnetCoreVersion);
                }
            }
        }

        /// <summary>
        /// Adds the NETCore App version as a measure.
        /// </summary>
        public static void MeasureNetCoreAppVersion()
        {
            var netCoreAppVersion = typeof(object).GetTypeInfo().Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;

            if (netCoreAppVersion != null)
            {
                Log.MeasureString("NetCoreAppVersion", netCoreAppVersion);
            }
        }

        [Event(1, Level = EventLevel.Informational)]
        public void MeasureLong(string name, long value)
        {
            WriteEvent(1, name, value);
        }

        [Event(2, Level = EventLevel.Informational)]
        public void MeasureDouble(string name, double value)
        {
            WriteEvent(2, name, value);
        }

        [Event(3, Level = EventLevel.Informational)]
        public void MeasureString(string name, string value)
        {
            WriteEvent(3, name, value);
        }

        [Event(5, Level = EventLevel.Informational)]
        public void Metadata(string name, string aggregate, string reduce, string shortDescription, string longDescription, string format)
        {
            WriteEvent(5, name, aggregate, reduce, shortDescription, longDescription, format);
        }
    }
}
