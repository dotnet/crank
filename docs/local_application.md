## Description

This tutorial explains how to send local source files to an agent instead of cloning from a public repository.

## Prerequisites

1. You should have followed the [Getting Started](getting_started.md) tutorial, and have `crank` and `crank-agent` tools available;
2. You have a local clone of the Crank repository;
3. The agent is running locally.

## Define the scenario

In the [Getting Started](getting_started.md) tutorial, the **hello** application is benchmarked. It's `source` section is pointing to the Crank repository to inform the Crank Agent to clone it and build it.

```yml
  server:
    source:
      repository: https://github.com/dotnet/crank
      branchOrCommit: main
      project: samples/hello/hello.csproj
    readyStateText: Application started.
```

In cases where you want to iterate quickly on an application, it's easier to do changes locally and send the source file to the agent instead of having it clone changes what you would have to push.

The `source` property of a job has a `localFolder` property that can be set to a local folder. 

The file `/crank/samples/local/local.benchmarks.yml` demonstrates how to use this property instead.

```yml
  server:
    source:
      localFolder: ../../samples/hello
      project: hello.csproj
    readyStateText: Application started.
```

The path is relative to the configuration file that contains it.

Run the following command line to execute this job.

```
> crank --config /crank/samples/local/local.benchmarks.yml --scenario hello --profile local
```

With these new arguments, Crank will zip and upload the contents of the `/crank/samples/hello` folder when the scenario is executed. The agent won't have to clone a repository.

## Testing local changes

Open the file `/crank/samples/hello/Program.cs` and change the `Main` method like this:

```c#
public static void Main(string[] args)
{
    Console.WriteLine("Hello");

    CreateHostBuilder(args).Build().Run();
}
```

Execute the following command:

```
> crank --config /crank/samples/local/local.benchmarks.yml --scenario hello --profile local --application.options.displayOutput true
```

Notice the `--application.options.displayOutput` argument which will stream the output of the application from the agent:

```
[application] Hello
[application] info: Microsoft.Hosting.Lifetime[0]
[application]       Now listening on: http://10.0.0.102:5000
[application] info: Microsoft.Hosting.Lifetime[0]
[application]       Application started. Press Ctrl+C to shut down.
[application] info: Microsoft.Hosting.Lifetime[0]
[application]       Hosting environment: Production
[application] info: Microsoft.Hosting.Lifetime[0]
[application]       Content root path: /tmp/benchmarks-agent/benchmarks-server-6/32kirmdq.ulk/src/published
```

## Conclusion

Use this technique to iterate quickly when benchmarking local changes. You can even do more changes while the benchmark is running remotely.
