// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using Microsoft.Crank.Models;

namespace Microsoft.Crank.RegressionBot.Models
{
    public class Regression
    {
        // The result before the one that is considered a regression
        public BenchmarksResult PreviousResult { get; set; }
        
        // The first result that is considered a regression
        public BenchmarksResult CurrentResult { get; set; }

        public double Change { get; set; }
        
        public double StandardDeviation { get; set; }
        
        public double Average { get; set; }

        // Whether the regression is now fixed
        [MessagePack.IgnoreMember]
        public bool HasRecovered => RecoveredResult != null;

        // The result when the benchmark recovered
        public BenchmarksResult RecoveredResult { get; set; }

        /// <summary>
        /// Gets a string representing this regression.
        /// Used to determine if two regressions are similar.
        /// </summary>
        public string Identifier => $"Id:{CurrentResult.Scenario}{CurrentResult.Description}{CurrentResult.DateTimeUtc}";

        public HashSet<string> Labels { get; set; } = new HashSet<string>();
        public HashSet<string> Owners { get; set; } = new HashSet<string>();

        public List<DependencyChange> Changes { get; set; } = new List<DependencyChange>();

        /// <summary>
        /// Calculates the diffs between dependencies
        /// </summary>
        public void ComputeChanges()
        {
            // If we are using results with no dependencies (old results, or dependencies not gathered) we generate the ones for dotnet

            var previous = JsonSerializer.Deserialize<JobResults>(PreviousResult.Document, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            var current = JsonSerializer.Deserialize<JobResults>(CurrentResult.Document, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            foreach (var jobName in previous.Jobs.Keys)
            {
                // The job needs to exist in both results
                if (!current.Jobs.ContainsKey(jobName))
                {
                    continue;
                }

                var previousJob = previous.Jobs[jobName];
                var currentJob = current.Jobs[jobName];

                // Do we need to create dependencies for aspnet and dotnet core? 
                if (previousJob.Dependencies.Length == 0)
                {
                    CreateDotNetDependencies(previousJob);
                }

                // Do we need to create dependencies for aspnet and dotnet core? 
                if (currentJob.Dependencies.Length == 0)
                {
                    CreateDotNetDependencies(currentJob);
                }

                // Group all dependencies by id for easy matching
                var previousDependenciesById = previousJob.Dependencies.GroupBy(x => x.Id).ToDictionary(x => x.Key, x => x.FirstOrDefault());
                var currentDependenciesById = currentJob.Dependencies.GroupBy(x => x.Id).ToDictionary(x => x.Key, x => x.FirstOrDefault());

                foreach (var id in currentDependenciesById.Keys)
                {
                    if (!previousDependenciesById.TryGetValue(id, out var previousDependency))
                    {
                        // This is a new dependency, ignore for now
                        continue;
                    }

                    var currentDependency = currentDependenciesById[id];

                    // Is there a change ?
                    if (currentDependency.Version != previousDependency.Version || currentDependency.CommitHash != previousDependency.CommitHash)
                    {
                        var change = new DependencyChange
                        {
                            Id = id,
                            Job = jobName,
                            Names = previousDependency.Names,
                            RepositoryUrl = previousDependency.RepositoryUrl,
                            PreviousVersion = previousDependency.Version,
                            PreviousCommitHash = previousDependency.CommitHash,
                            CurrentVersion = currentDependency.Version,
                            CurrentCommitHash = currentDependency.CommitHash,
                            ChangeType = ChangeTypes.Diff
                        };

                        Changes.Add(change);
                    }
                }

                // Detect new groups
                foreach (var id in currentDependenciesById.Keys.Except(previousDependenciesById.Keys))
                {
                    var currentDependency = currentDependenciesById[id];

                    var change = new DependencyChange
                    {
                        Id = id,
                        Job = jobName,
                        Names = currentDependency.Names,
                        RepositoryUrl = currentDependency.RepositoryUrl,
                        PreviousVersion = "",
                        PreviousCommitHash = "",
                        CurrentVersion = currentDependency.Version,
                        CurrentCommitHash = currentDependency.CommitHash,
                        ChangeType = ChangeTypes.New
                    };

                    Changes.Add(change);
                }

                // Detect deleted groups
                foreach (var id in previousDependenciesById.Keys.Except(currentDependenciesById.Keys))
                {
                    var previousDependency = previousDependenciesById[id];

                    var change = new DependencyChange
                    {
                        Id = id,
                        Job = jobName,
                        Names = previousDependency.Names,
                        RepositoryUrl = previousDependency.RepositoryUrl,
                        PreviousVersion = previousDependency.Version,
                        PreviousCommitHash = previousDependency.CommitHash,
                        CurrentVersion = "",
                        CurrentCommitHash = "",
                        ChangeType = ChangeTypes.Removed
                    };

                    Changes.Add(change);
                }
            }

            void CreateDotNetDependencies(JobResult jobResult)
            {
                if (jobResult.Results.TryGetValue("aspNetCoreVersion", out var aspnetCoreVersion))
                {
                    var versionSegments = Convert.ToString(aspnetCoreVersion).Split('+', 2, StringSplitOptions.RemoveEmptyEntries);

                    jobResult.Dependencies = jobResult.Dependencies.Append(
                        new Dependency
                        {
                            Id = "+kL3IPaqvdVHIVR8mUBvrw==",
                            Names = new[] { "Microsoft.AspNetCore.App" },
                            RepositoryUrl = "https://github.com/dotnet/aspnetcore",
                            Version = versionSegments.FirstOrDefault(),
                            CommitHash = versionSegments.Skip(1).FirstOrDefault()
                        }).ToArray();
                }

                if (jobResult.Results.TryGetValue("netCoreAppVersion", out var netCoreAppVersion))
                {
                    var versionSegments = Convert.ToString(netCoreAppVersion).Split('+', 2, StringSplitOptions.RemoveEmptyEntries);

                    jobResult.Dependencies = jobResult.Dependencies.Append(
                        new Dependency
                        {
                            Id = "VQgr9CxNHDwaUGAf9ff0Tw==",
                            Names = new[] { "Microsoft.NETCore.App" },
                            RepositoryUrl = "https://github.com/dotnet/runtime",
                            Version = versionSegments.FirstOrDefault(),
                            CommitHash = versionSegments.Skip(1).FirstOrDefault()
                        }).ToArray();
                }
            }
        }
    }
}
