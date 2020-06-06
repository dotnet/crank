## Description

This tutorial explains how to send local source files to an agent instead of cloning from a public repository.

## Prerequisites

1. You should have followed the [Getting Started](getting_started) tutorial, and have `crank` and `crank-agent` tools available.
2. You have a local clone of the Crank repository
3. The agent is running locally

## Define the scenario

In the [Getting Started](getting_started.md) tutorial, the **hello** application is benchmarked. It's `source` section is pointing to the Crank repository to inform the Crank Agent to clone it and build it.

```yml
  server:
    source:
      repository: https://github.com/dotnet/crank
      branchOrCommit: master
      project: samples/hello/hello.csproj
    readyStateText: Application started.
```

In cases where you want to iterate quickly on an application, it's easier to do changes locally and send the source file to the agent instead of having it clone changes what you would have to push.

The `source` property of a job has a `localFolder` property that can be set to a local folder. 

The following command line is altering the scenario by redefining the `source` property of the **server** job. Because we are only sending the folder containing the project, we also need to redefine the project file location relative to the new root.

```
> crank --config /crank/samples/hello/hello.benchmarks.yml --scenario hello --profile local --server.source.localFolder "/crank/samples/hello" --server.source.project "hello.csproj"
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
> crank --config /crank/samples/hello/hello.benchmarks.yml --scenario hello --profile local --application.source.localFolder "/crank/samples/hello" --application.source.project "hello.csproj" --application.options.displayOutput true
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