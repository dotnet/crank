## Description

This guide shows how to execute custom scripts over the results and measurements.

Once a benchmark is finished, the results (computed values out of measurements) and the data points (timestamped measurements) can be saved locally or in a database. However it might be necessary to compute custom results, alter existing ones, or even add custom properties.

Crank provides the ability to define custom post-processing scripts that can be invoked before the results are stored, and reused across runs.

In these scripts, the `benchmarks` property is available for read/write access, and represents the JSon document as it would be saved on disk with the option `--json [filename]`.

## Prerequisites

1. You should have followed the [Getting Started](getting_started.md) tutorial, and have `crank` and `crank-agent` tools available.


## Global scripts

A global script is one that is executed automatically when a configuration file is loaded. It can be useful when named scripts need to share some common function or variables.
The following script creates a function to compute percentiles of a set of values.

__percentile.config.yml__

```yml
onResultsCreating:
  - |
    console.log("hi there")

    function percentile(items, th) {
      var ordered = items.sort((a, b) => a - b); // by default sort() uses ordinal comparison
      index = Math.max(0, Math.round(ordered.length * th / 100) - 1);
      return ordered[index];
    }
```

By running a benchmark with the `--config percentile.config.yml` the text _hi there_ would be displayed, and the `percentile()` function would be available for any other scripts that are invoked.

> Note the `|` in yaml that marks the beginning of a multi-line block of text.

## Named scripts

Named scripts are defined in the `scripts` section of a configuration file. They are opt-in, meaning that contrary to _global scripts_ and _post-processing scripts_ they won't be executed
automatically once the configuration file is loaded.

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

## Post-processing scripts

```yml
results: # creates results from measurements

- name: alloc-per-request
  description: Allocations Per request (B/req)
  format: n2

onResultsCreated:
  - |
    var allocations = benchmarks.jobs.application.results["runtime-counter/alloc-rate"]
    var rps = benchmarks.jobs.load.results["http/rps/mean"];
    benchmarks.jobs.application.results["alloc-per-request"] = allocations / rps;
```

## Logging

A custom `console` object is made available and support the following methods:

- `log(message)`
- `info(message)`
- `warn(message)`
- `error(message)`

These methods will use different colors to render the message, respectively neutral, green, yellow and red.


## Files

A custom `fs` object is made available and support the following methods:

- `readFile(filename) : string`
- `writeFile(filename, data)`
- `exists(filename) : bool`

