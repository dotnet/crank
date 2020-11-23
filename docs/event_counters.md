## Description

This guide shows how to collect standard and custom dotnet event counters.

Crank is able to record any predefined set of event counters, the same way [dotnet-counters](https://docs.microsoft.com/en-us/dotnet/core/diagnostics/dotnet-counters) does, but without the need of spawning a different sidecar process. Another advantage over dotnet-counters is that it can also be configured to record custom event counters and apply build custom statistics out of it (max, min, percentiles, ...).

## Prerequisites

1. You should have followed the [Getting Started](getting_started.md) tutorial, and have `crank` and `crank-agent` tools available.

## Collecting event counters

The following command line will run a benchmark and record the event counters exposed by the `System.Runtime` provider.

```
crank --config /crank/samples/hello/hello.benchmarks.yml --scenario hello --profile local --application.counterProviders System.Runtime --chart
```

```
| application                   |            |                                     |
| ----------------------------- | ---------- | ----------------------------------- |
| CPU Usage (%)                 | 38         |           ▃▇▇▇▆▆▆▇▆▆▆▅█▇▇█▇▇▇▅▃     |
| Raw CPU Usage (%)             | 300.46     |           ▃▇▇▇▆▆▆▇▆▆▆▅█▇▇█▇▇▇▆▃     |
| Working Set (MB)              | 89         | ▃▃▃▃▃▃▃▃▃▃▆▇████████████████████    |
| Build Time (ms)               | 5,358      |                                     |
| Start Time (ms)               | 564        |                                     |
| Published Size (KB)           | 86,753     |                                     |
| .NET Core SDK Version         | 5.0.100    |                                     |
| Max CPU Usage (%)             | 35         |            ▂█▇█▆▆██▆█▅▇▇▇███▇█▇▅    |
| Max Working Set (MB)          | 93         | ▃▃▃▃▃▃▃▃▃▃▃▅▇▇█████████████████████ |
| Max GC Heap Size (MB)         | 39         |            ▄▅▆▆▁▂▄▆▅▇▂▁▂▂▃▂▂█▅▆▂▂▂▂ |
| Max Number of Gen 0 GCs / min | 3.00       |             ▅▅▅█▅▅▅▅▅█▅▅▅▅▅▅▃▅▅▅    |
| Max Number of Gen 1 GCs / min | 1.00       |             █    █                  |
| Max Number of Gen 2 GCs / min | 1.00       |                  █                  |
| Max Time in GC (%)            | 1.00       |                             █       |
| Max Gen 0 Size (B)            | 192        |             ███████████████████████ |
| Max Gen 1 Size (B)            | 3,272,160  |                 █                   |
| Max LOH Size (B)              | 134,392    |             ███████████████████████ |
| Max Allocation Rate (B/sec)   | 72,372,048 |            ▂▇███▇██▇█▇▇▇██▇▇▇▆▆▅    |
| Max GC Heap Fragmentation     | NaN        |                                     |
| # of Assemblies Loaded        | 97         | ▇▇▇▇▇▇▇▇▇▇▇████████████████████████ |
| Max Exceptions (#/s)          | 1,019      |                 █              █    |
| Max Lock Contention (#/s)     | 34         |                 █▁▁ ▁  ▁  ▁ ▁▁▁█    |
| Max ThreadPool Threads Count  | 23         | ▁▁▁▁▁▁▁▁▁▁▁▆▆▆▆▆██▆▆█▆▆▇▆▆▇▆▇▆▇▇▇▇▇ |
| Max ThreadPool Queue Length   | 101        |            █  ▁▃   ▁     ▁  ▁       |
| Max ThreadPool Items (#/s)    | 191,763    |            ▁▆██▇▆██▇█▇▇▇████▇▆▆▅    |
| Max Active Timers             | 0          |                                     |
| IL Jitted (B)                 | 168,412    | ▁▂▂▂▂▂▂▂▂▂▂▃▆▆▆▆▆▇▇▇▇▇▇▇▇▇▇▇▇▇▇▇███ |
| Methods Jitted                | 1,927      | ▁▂▂▂▂▂▂▂▂▂▂▃▆▆▆▆▆▇▇▇▇▇▇▇▇▇▇▇▇▇▇▇███ |
```

The chart on the last column are created from all the data points over the benchmark.

## List of pre-defined counter providers:

The following providers are pre-defined in crank:

- System.Runtime
- Microsoft-AspNetCore-Server-Kestrel
- Microsoft.AspNetCore.Hosting
- System.Net.Http
- Microsoft.AspNetCore.Http.Connections
- Grpc.AspNetCore.Server
- Grpc.Net.Client
- Npgsql

## Adding custom providers

The providers are defined in the same config files that contain the scenarios of job definitions, under the `counters` section.

Here is an example defining a subset of the `System.Runtime` counters:

```yml
counters:
- provider: System.Runtime
  values: 
  - name: cpu-usage
    measurement: runtime-counter/cpu-usage
    description: Percentage of time the process has utilized the CPU (%)

  - name: working-set
    measurement: runtime-counter/working-set
    description: Amount of working set used by the process (MB)
```

Using this format, you can add the definition of a counter you are exposing, and all its values will be recorded as measurements.

## Building results out of measurements

When an event counter is recorded, it creates timestamped measurements. There can be multiple data points of the same measurement. To build a result that is displayed in the summary table, and saved in results files, you need to define a __result__ that will desribe how to transform the set of measurements in a single value, usually the max.

In any configuration file, results can be defined under the `results` section like this:

```yml
results:

# System.Runtime counters
- name: runtime-counter/cpu-usage
  measurement: runtime-counter/cpu-usage
  description: Max CPU Usage (%)
  format: n0
  aggregate: max
  reduce: max
  
- name: runtime-counter/working-set
  measurement: runtime-counter/working-set
  description: Max Working Set (MB)
  format: n0
  aggregate: max
  reduce: max
```

Multiple results can be computed for the same measurements. The following example adds a new result that computes the 95th of the working set:

```yml
- name: runtime-counter/working-set/95
  measurement: runtime-counter/working-set
  description: Max Working Set (MB)
  format: n0
  aggregate: percentile95
  reduce: max
```

- The `aggregate` operation defines how to group the results that come from a single source.
- The  `reduce` operation defines how to group the aggregated results from different sources. This is less usual as it requires a job to run on multiple nodes, for instance when splitting a web load on multiple clients.

### Available operations

The following operations are available by default:

- max
- min
- last
- first
- all
- avg
- sum
- median
- count
- delta
- percentile99
- percentile95
- percentile90
- percentile75
- percentile50

## Defining custom operations

Any configuration file can add custom operations using javascript, in the `defaultScript` section.

The following example defines the `max` operation:

```yml
defaultScripts:
  - |
    function max(values) {
      return Math.max(...values);
    }
```
