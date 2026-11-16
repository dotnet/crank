## Description

This guide describes how to benchmark the same application on different .NET versions. It's using the same services as in [Getting Started](getting_started.md).

## Switching TFMs

TFMs ([Target Framework Moniker](https://docs.microsoft.com/en-us/dotnet/standard/frameworks)) in a .NET project allow for multi-target deployment of an app.

By default **crank** will deploy a .NET application using the first TFM that is specified in the project file.

The **hello** sample application targets both `netcoreapp3.1` and `netcoreapp5.0`.

```xml
<TargetFrameworks>netcoreapp3.1;netcoreapp5.0</TargetFrameworks>
```

When running this command line, the TFM `netcoreapp3.1` is used.

```
> crank --config /crank/samples/hello/hello.benchmarks.yml --scenario hello --profile local
```

To force the application to use `netcoreapp5.0` instead, use the `framework` property of the job. This can be set directly in the `.yml` configuration file, or using a command line argument like this:

```
> crank --config /crank/samples/hello/hello.benchmarks.yml --scenario hello --profile local --application.framework netcoreapp5.0
```

The argument makes use of the name of the service as a benchmark can depend on multiple services. 

## Verifying which versions were used

When **crank** deploys each service on an agent, it output a url that can be used to query a JSON representation of its state.

```
[10:54:53.142] Starting job 'application' ...
[10:54:53.199] Fetching job: http://localhost:5010/jobs/3
```

In this example the resulting document would contain these properties with the `netcoreapp3.1` TFM:

```json
{
    "aspNetCoreVersion": "3.1.8",
    "runtimeVersion": "3.1.8",
    "sdkVersion": "3.1.402"
}
```

## Switching framework versions

When a TFM is configured, the agent will download the corresponding .NET SDK version and use the latest public shared runtimes to run the application.

**crank** is also able to use any version of a .NET runtime using the notion of **channels**. The values can be:
- `current`: only latest public versions, this is the default
- `latest`: latest versions used by ASP.NET 
- `edge`: latest nightly builds available

The difference between `latest` and `edge` is that `latest` will pick runtimes and SDKs that are deemed compatible together. For instance a very recent .NET core runtime might be compatible with a less recent ASP.NET runtime. The `edge` is used to pick the absolute latest build for the select TFM.

In order to benchmark and ASP.NET application using very recent runtimes of .NET 5, the `latest` channel is recommended:

```
> crank --config /crank/samples/hello/hello.benchmarks.yml --scenario hello --profile local --application.framework netcoreapp5.0 --application.channel latest
```

The following values are gathered with the **current** channel. They represent runtimes and SDKs that are available as public preview releases usually published on NuGet.org. 

```json
{
    "aspNetCoreVersion": "5.0.0-preview.4.20257.10",
    "runtimeVersion": "5.0.0-preview.4.20251.6",
    "sdkVersion": "5.0.100-preview.4.20258.7"
}
```

When using the **latest** channel we enlist for nightly build versions which vary much more frequently. However the .NET Core runtime and SDK versions might represent the very latest build available, only the ones that ASP.NET is currently using. 

```json
{
    "aspNetCoreVersion": "5.0.0-preview.6.20279.12",
    "runtimeVersion": "5.0.0-preview.6.20278.9",
    "sdkVersion": "5.0.100-preview.6.20266.3"
}
```

Finally, with the **edge** channel, all versions represent the latest available continuous builds.

```json
{
    "aspNetCoreVersion": "5.0.0-preview.6.20279.12",
    "runtimeVersion": "5.0.0-preview.6.20301.4",
    "sdkVersion": "5.0.100-preview.6.20301.7"
}
```

## Specifying different channels

Channels can be set individually on each component including
- ASP.NET runtime with `aspNetCoreVersion`
- .NET Core runtime (CLR) with `runtimeVersion`
- SDK with `sdkVersion`

The following example uses the default channel for ASP.NET but forces to use the most recent runtime.

```
> crank --config /crank/samples/hello/hello.benchmarks.yml --scenario hello --profile local --application.framework netcoreapp5.0 --application.runtimeVersion edge
```

## Specifying specific versions

Using channels provides a way to always be using recent versions. However when comparing benchmarks we might need to used fixed version numbers to be sure no external changes might be responsible for a variation. For instance when checking for a CLR improvement it's recommended to set a fixed ASP.NET version across runs. Specific versions can be used together with channels.

The following command uses the `edge` channel but ASP.NET is fixed so it doesn't vary over time.

```
> crank --config /crank/samples/hello/hello.benchmarks.yml --scenario hello --profile local --application.framework netcoreapp5.0 --application.channel edge --application.aspnetCoreVersion 5.0.0-preview.6.20279.12
```