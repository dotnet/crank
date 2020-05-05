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
cd /perf/src/Microsoft.Benchmarks.Controller
dotnet run -- --config https://raw.githubusercontent.com/aspnet/perf/master/samples/hello/hello.benchmarks.yml?token=AAI4T3M6P23AMHKCEI2QAPS6XMT7S --scenario hello --profile
```

#### Optional: Storing the results

The controller can store the results of a job by passing a `-q [connectionstring]` argument. The connection
string must point to an existing SQL Server database. The first time it's called the required table will be created.
From there you can create reports using the tools of your choice.
