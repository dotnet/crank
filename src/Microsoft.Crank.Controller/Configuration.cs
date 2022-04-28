// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using Microsoft.Crank.Models;

namespace Microsoft.Crank.Controller
{
    public class Configuration
    {
        public Dictionary<string, object> Variables { get; } = new ();

        public Dictionary<string, Job> Jobs { get; } = new ();

        public Dictionary<string, Dictionary<string, Service>> Scenarios { get; } = new ();

        public Dictionary<string, object> Profiles { get; } = new ();

        /// <summary>
        /// List of named script sections that can be executed in a run
        /// </summary>
        public Dictionary<string, string> Scripts { get; } = new ();

        /// <summary>
        /// Scripts which are loaded automatically when the configuration file is included.
        /// It's a collection such that multiple configuration files can be merged without overwriting the scripts. 
        /// </summary>
        [Obsolete("Use OnResultsCreating instead")]
        public List<string> DefaultScripts { get; set; } = new List<string>();

        /// <summary>
        /// Scripts which are loaded automatically when the configuration file is included.
        /// It's a collection such that multiple configuration files can be merged without overwriting the scripts. 
        /// </summary>
        public List<string> OnResultsCreating { get; set; } = new List<string>();

        /// <summary>
        /// .NET counters that are available during a benchmark.
        /// </summary>
        public List<CounterList> Counters { get; set; } = new List<CounterList>();

        /// <summary>
        /// Computed results definitions.
        /// </summary>
        public List<Result> Results { get; set; } = new List<Result>();

        /// <summary>
        /// Scripts to run when the results are computed.
        /// </summary>
        public List<string> OnResultsCreated { get; set; } = new List<string>();

    }

    public class Service
    {
        public string Job { get; set; }

        /// The name of the service defined in a profile.
        public string Agent { get; set; }
    }

    public class CounterList
    {
        /// <summary>
        /// The name of the dotnet counters list, e.g. System.Runtime
        /// </summary>
        public string Provider { get; set; } 

        public List<Counter> Values { get; set; } = new List<Counter>();
    }

    public class Counter
    {
        /// <summary>
        /// The name of the counter
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// The name of the measurement in the results
        /// </summary>
        public string Measurement { get; set; }

        /// <summary>
        /// The description of the counter
        /// </summary>
        public string Description { get; set; }
    }

    public class Result
    {
        /// <summary>
        /// The name of the measurements sent back from the jobs
        /// </summary>
        public string Measurement { get; set; }

        /// <summary>
        /// The name of the result to create
        /// </summary>
        public string Name { get; set; }
        public string Description { get; set; }
        
        public string Format { get; set; }
        
        public string Aggregate { get; set; }
        
        public string Reduce { get; set; }

        public bool Excluded { get; set; }
    }
}
