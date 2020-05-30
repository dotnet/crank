## Description

This tutorial shows how to benchmark a simple .NET web application using the __bombardier__ load generation tool.

## Installing crank

1. Install [.NET Core 3.1](<http://dot.net>).
2. Install Crank via the following command:

    ```text
    dotnet tool install -g Microsoft.Crank.Controller --version "0.1.0-*" 
    ```

    ```text
    dotnet tool install -g Microsoft.Crank.Agent --version "0.1.0-*" 
    ```

NB: Preview versions can be installed by adding this NuGet feed source: `--add-source https://pkgs.dev.azure.com/dnceng/public/_packaging/dotnet-tools/nuget/v3/index.json`

3. Verify the installation was complete by running:

    ```
    crank
    > No jobs were found. Are you missing the --scenario argument?
    ```

## Define the scenario

The following content is available at https://github.com/dotnet/crank/blob/master/samples/hello/hello.benchmarks.yml

It contains the scenario definitions, describing which applications need to be deployed to run a benchmark.

```yml
imports:
    - https://raw.githubusercontent.com/aspnet/Benchmarks/master/src/BombardierClient/bombardier.yml

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
          - http://localhost:5010
```
## Start the agent

To run the benchmark at least one agent instance needs to be running. It will run the service named  __application__ that will host the web application to benchmark, and the service name __load__ that will host the bombardier load generation.

Ideally, services are deployed on distinct machines but for the sake of this tutorial as single agent is used.

Open a shell and execute these command lines:

```
> crank-agent
...
Now listening on: http://[::]:5010
...
Agent ready, waiting for jobs...
```

At that point the agent is ready to accept jobs locally on the port `5010`.

## Run a scenario using the controller

The scenario definitions file is already created and available.

```
> crank --config /crank/samples/hello/hello.benchmarks.yml --scenario hello --profile local

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

Each service deployed (application and load) have reported their metrics, including the Requests Per Second.

### Optional: Storing the results

The controller can store the results of a job either in JSON formar or in a SQL Server database.

Use `--save results.json` to store the results in JSON format. In the case of the bombardier client, this will also contain useful latency information.

Use `--sql [connectionstring] --table [tablename]` arguments to store in the specified SQL Server database. The connection string must point to an existing SQL Server database. The first time it's called the required table will be created.

From there you can create reports using the tools of your choice.
