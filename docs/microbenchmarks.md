## Description

This tutorial explains how to run [BenchmarkDotNet](https://benchmarkdotnet.org/) micro-benchmarks.

The advantages of running BenchmarkDotNet with **crank** are:

- more stable results since local applications can't impact the results
- shareable results as all developers can use the same machines
- don't block your machine while it runs
- apply specific constraints on the machine: cpu, memory
- benchmark local builds
- store the results in SQL Server

## Prerequisites

1. You should have followed the [Getting Started](getting_started.md) tutorial, and have `crank` and `crank-agent` tools available;
2. You have a local clone of the Crank repository;
3. The agent is running locally.

## Running the sample

Assuming the crank agent is running locally, you can execute the following command line:

```
> crank --config /crank/samples/micro/micro.benchmarks.yml --scenario Md5VsSha256 --profile local

...

BenchmarkDotNet=v0.12.1, OS=Windows 10.0.18363.1139 (1909/November2018Update/19H2)
Intel Core i7-3667U CPU 2.00GHz (Ivy Bridge), 1 CPU, 4 logical and 2 physical cores
.NET Core SDK=  3.1.300 [C:\Program Files\dotnet\sdk]
  [Host] : .NET Core 3.1.9 (CoreCLR 4.700.20.47201, CoreFX 4.700.20.47203), X64 RyuJIT

Toolchain=InProcessEmitToolchain  IterationCount=3  LaunchCount=1
WarmupCount=3

 Method |   N |     Mean |      Error |    StdDev |  Gen 0 | Gen 1 | Gen 2 | Allocated |
------- |---- |---------:|-----------:|----------:|-------:|------:|------:|----------:|
 Sha256 | 100 | 868.6 ns | 3,196.4 ns | 175.21 ns | 0.0114 |     - |     - |     112 B |
    Md5 | 100 | 466.1 ns |   138.2 ns |   7.58 ns | 0.0086 |     - |     - |      80 B |
 Sha256 | 500 | 791.2 ns |   346.0 ns |  18.97 ns | 0.0114 |     - |     - |     112 B |
    Md5 | 500 | 475.8 ns |   260.4 ns |  14.27 ns | 0.0081 |     - |     - |      80 B |
```

**crank** locally outputs the results that BenchmarkDotNet generated when running the benchmarks on the Agent. 

## Define the scenario

The following example is available at https://github.com/dotnet/crank/blob/main/samples/micro/micro.benchmarks.yml

```yml
  benchmarks:
    sources:
      micro:
        localFolder: .
    project: micro/micro.csproj
    variables:
      filterArg: "*"
      jobArg: short
    arguments: --job {{jobArg}} --filter {{filterArg}} --memory
    options:
      benchmarkDotNet: true
```

This job is based on the `micro.csproj` project that contains a sample micro-benchmark measuring the differences between **Md5** and **Sha256** hashing algorithms.

The main difference between a normal job and one that uses BenchmarkDotNet, is the `options.benchmarkDotNet` property that is set to `true`. By doing so the crank controller will set some default arguments, and know it has to download the report and display it.

Another requirement is that the application needs to flow the command line arguments like this:

```c#
public static void Main(string[] args)
{
    BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).Run(args);
}
```

## Customizing benchmarks

Custom arguments can be defined at the job level, or at the scenario level. For instance the **micro** sample defines two variables, and adds a default memory **exporter**.

```yml
variables:
  filterArg: "*"
  jobArg: short
arguments: --job {{jobArg}} --filter {{filterArg}} --memory
```

By doing this, each scenario can change the value of these variables.
More information about the available command line arguments can be found [on the BenchmarkDotNet website](https://benchmarkdotnet.org/articles/guides/console-args.html)

## Storing results locally

Like any other **crank** job, the `--output [filename]` argument can be used. However the format will differ from standard jobs, and use the *json brief* format from BenchmarkDotNet.

See [this example](https://benchmarkdotnet.org/articles/samples/IntroExportJson.html#output) from the BenchmarkDotNet website.

## Running dotnet/performance benchmarks

The https://github.com/dotnet/performance repository contains several micro-benchmarks based on BenchmarkDotNet. The file [dotnet.benchmark.yml](https://github.com/dotnet/crank/blob/main/samples/micro/dotnet.benchmarks.yml) contains an example scenario that can be used directly.

For instance to run the sockets micro-benchmarks, use this command:

```
> crank --config /crank/samples/micro/dotnet.benchmarks.yml --scenario Sockets --profile local
```

The file points directly to the GitHub repository, and defines a `filterArg` argument that will only use the expected classes. You can follow this example to target other benchmarks from this repository.

## Conclusion

Crank can be used to run BenchmarkDotNet micro-benchmarks remotely, including the existing ones from https://github.com/dotnet/performance.
