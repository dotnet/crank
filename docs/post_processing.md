## Description

This guide shows how to execute custom scripts over the results and measurements.

Once a benchmark is finished, the results (compured values our of measurements) and the data points (timestamped measurements) can be saved locally or in a database. However it might be necessary to compute custom results, alter existing ones, or even add custom properties.

Crank provides the ability to define custom post-processing scripts that can be invoked before the results are stored, and reused across runs.

## Prerequisites

1. You should have followed the [Getting Started](getting_started.md) tutorial, and have `crank` and `crank-agent` tools available.

## Defining custom scripts

Custom scripts are defined in the `scripts` section of a configuration file.
In a script, the `benchmarks` property is available for read/write access, and represent the JSon document as it would be save on disk with the option `--output [filename]`.

The following configuration snippet demonstrates how to add a custom property to the `properties` element of the results:

```yml
scripts:
  add_current_time: |
    benchmarks.properties["time"] = new Date().toISOString();
```

Custom scripts are invoked from the command line with the `--script` option like so:

```
crank --config /crank/samples/hello/hello.benchmarks.yml --scenario hello --profile local --script add_current_time
```

Multiple scripts can be invoked from the command line by invoking the argument multiple times.
All the script are executed in the same JavaScript context, such that top level variables are shared across script invocations.

## Defining global scripts

A global script is one that is executed automatically when a configuration file is loaded. It can be useful when named scripts need to share some common function or variables.
The following script creates a function to compute percentiles of a set of values.

__percentile.config.yml__

```yml
defaultScript:
  - |
    console.log("this section is loaded by default and before named scripts")

    function percentile(items, th) {
      var ordered = items.sort((a, b) => a - b); // by default sort() uses ordinal comparison
      index = Math.max(0, Math.round(ordered.length * th / 100) - 1);
      return ordered[index];
    }
```

By running a benchmark with the `--config percentile.config.yml` the text _this section is loaded by default and before named scripts_ would be displayed, and the `percentile()` function would be available for any other scripts that are invoked.

## Adding custom results

Some values might be the results of results coming from different source. The following examples show how to add a new result to the __application__ job by using one that is in the __load__ job.

```yml
  add_allocations_per_request: |
    var allocations = benchmarks.jobs.application.results["runtime-counter/alloc-rate"]
    var rps = benchmarks.jobs.load.results["wrk/requests"];
    benchmarks.jobs.application.results["alloc-per-request"] = allocations / rps;
```

## Logging

A custom `console` object is made available and support the following methods:

- log(message)
- info(message)
- warn(message)
- error(message)

These methods will use different colors to render the message, respectively default, green, yellow and red.
