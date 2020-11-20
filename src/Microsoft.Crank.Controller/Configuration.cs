// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using Microsoft.Crank.Models;

namespace Microsoft.Crank.Controller
{
    public class Configuration
    {
        public object Variables { get; set; } = new Dictionary<string, object>();

        public Dictionary<string, Job> Jobs { get; set; } = new Dictionary<string, Job>();

        public Dictionary<string, Dictionary<string, Scenario>> Scenarios { get; set; } = new Dictionary<string, Dictionary<string, Scenario>>();

        public Dictionary<string, object> Profiles { get; set; } = new Dictionary<string, object>();

        /// <summary>
        /// List of named script sections that can be executed in a run
        /// </summary>
        public Dictionary<string, string> Scripts { get; set; } = new Dictionary<string, string>();

        /// <summary>
        /// Scripts which are loaded automatically when the configuration file is included.
        /// It's a collection such that multiple configuration files can be merged without overwriting the scripts. 
        /// </summary>
        public List<string> DefaultScripts { get; set; } = new List<string>();

        /// <summary>
        ///  List of named script sections that can be executed in a run
        /// </summary>
        public List<CounterList> Counters { get; set; } = new List<CounterList>();

        /// <summary>
        ///  List of named script sections that can be executed in a run
        /// </summary>
        public List<Result> Results { get; set; } = new List<Result>();
    }

    public class Scenario
    {
        public string Job { get; set; }
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
