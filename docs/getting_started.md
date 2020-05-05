## Description

This tutorial shows how to benchmark a simple .NET web application using the __bombardier__ load generation tool.

### Define the scenario

The following content is available at https://github.com/aspnet/perf/blob/master/samples/hello/hello.benchmarks.yml

It contains the scenario definitions, describing which applications need to be deployed to run a benchmark.

```yml
imports:
    - https://raw.githubusercontent.com/aspnet/perf/master/src/Microsoft.Benchmarks.Jobs.Bombardier/bombardier.yml?token=AAI4T3PMOX4HXFZA3HQXTBK6XMTLO

jobs:
  server:
    source:
      repository: https://github.com/aspnet/perf
      branchOrCommit: master
      project: samples/hello/hello.csproj

scenarios:
  hello:
    application:
      job: server
    load:
      job: bombardier
      variables:
        serverPort: 5000
        path: /

profiles:
  local:
    variables:
      serverUri: http://localhost
    jobs: 
      application:
        endpoints: 
          - http://localhost:5010
      load:
        endpoints: 
          - http://localhost:5011
```

### Start the agents

To run the benchmark two agents instances need to be running. One for the deployment named  __application__ that will host the web application to benchmark, and one for the deployment name __load__ that will host the bombardier load generation. 

In two different shells, execute these command lines:

```
> cd /perf/src/Microsoft.Benchmarks.Agent
> dotnet run
...
Now listening on: http://[::]:5010
Application started. Press Ctrl+C to shut down.
...
```

```
> cd /perf/src/Microsoft.Benchmarks.Agent
> dotnet run
...
Now listening on: http://[::]:5011
Application started. Press Ctrl+C to shut down.
...
```

At that point the two agents are ready to accepts jobs locally on the ports `5010` and `5011`.

### Run a scenario using the controller

The scenario definitions file is already created and available.

```
> cd /perf/src/Microsoft.Benchmarks.Controller
> dotnet run -- --config /perf/samples/hello/hello.benchmarks.yml --scenario hello --profile local

[04:19:18.388] Running session 'bb96c510c041416c8fb576160ec12ea0' with description ''
[04:19:18.410] Starting job 'application' ...
[04:19:18.416] Fetching job: http://localhost:5010/jobs/1
[04:19:19.444] Job has been selected by the server ...
[04:19:19.445] Job is now building ...
[04:19:50.624] Job is running
[04:19:50.626] Starting job 'load' ...
[04:19:50.630] Fetching job: http://localhost:5011/jobs/1
[04:19:51.654] Job has been selected by the server ...
[04:19:51.655] Job is now building ...
[04:19:58.748] Job is running
[04:20:06.891] Stopping job 'load' ...
[04:20:07.912] Deleting job 'load' ...
[04:20:07.916] Stopping job 'application' ...
[04:20:09.922] Deleting job 'application' ...

application
-------

## Host Process:
CPU Usage (%):        49
Raw CPU Usage (%):    591.74
Working Set (MB):     152
Build Time (ms):      22,039
Published Size (KB):  86,543

load
-------

## Host Process:
CPU Usage (%):        2
Raw CPU Usage (%):    18.34
Working Set (MB):     31
Build Time (ms):      3,009
Published Size (KB):  68,072

## Benchmark:
Requests:             557,601
Bad responses:        0
Mean latency (us):    2,288
Max latency (us):     556,000
Max RPS:              221,956
```

Each deployment (application and load) has then reported their metrics, including the Requests Per Second.

#### Optional: Storing the results

The controller can store the results of a job either in JSON formar or in a SQL Server database.

Use `--save results.json` to store the results in JSON format. In the case of the bombardier client, this will also contain useful latency information.

Use `--sql [connectionstring] --table [tablename]` arguments to store in the specified SQL Server database. The connection string must point to an existing SQL Server database. The first time it's called the required table will be created.

From there you can create reports using the tools of your choice.
