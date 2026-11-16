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
- `ci`: base runtime + ASP.NET Core from the Build Cache Service, resolved per-commit (see below)

The difference between `latest` and `edge` is that `latest` will pick runtimes and SDKs that are deemed compatible together. For instance a very recent .NET core runtime might be compatible with a less recent ASP.NET runtime. The `edge` is used to pick the absolute latest build for the select TFM.

The `ci` channel uses the Build Cache Service (BCS) from `dotnet-performance-infra` to resolve framework versions by individual commit SHA rather than from VMR feeds. This provides much finer-grained control — every cached commit is available, whereas VMR feeds may have multi-day gaps between ingested commits. On this channel crank overrides **both** the base .NET runtime (`Microsoft.NETCore.App`, from dotnet/runtime) **and** the ASP.NET Core shared framework (`Microsoft.AspNetCore.App`, from dotnet/aspnetcore); each defaults to the latest cached build and can be pinned independently by passing a commit SHA in `runtimeVersion` / `aspNetCoreVersion`. SDK and desktop versions are resolved from `latest`.

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

## Using the ci channel

The `ci` channel resolves pre-built binaries for individual commits from the Build Cache Service (BCS). This is useful for performance regression bisection where VMR feed gaps make it hard to pinpoint which commit caused a regression.

On this channel crank always overrides **both** frameworks, each resolved from its own repository:

- **Base runtime** (`Microsoft.NETCore.App`) is **overlaid** with BCS bits built from a [dotnet/runtime](https://github.com/dotnet/runtime) commit. The runtime archive is raw build output (no shared-framework metadata), so BCS binaries are overlaid onto a feed-installed runtime.
- **ASP.NET Core shared framework** (`Microsoft.AspNetCore.App`) is **placed directly** from a [dotnet/aspnetcore](https://github.com/dotnet/aspnetcore) commit's BCS build. The aspnetcore archive is the runtime-pack nupkg stored verbatim (carrying `deps.json` + `runtimeconfig.json`), so the framework folder is built entirely from BCS and the job **fails** if the pack is incomplete.

Each repository resolves **independently**: by default both use the latest cached build on `main`. On the `ci` channel the existing `runtimeVersion` and `aspNetCoreVersion` arguments carry a **commit SHA** rather than a feed version — supply a SHA to pin/bisect one repo while the other stays latest, or leave it empty to use the latest cached build. The *latest* lookup always targets `main` (the pipeline only builds `main`).

> **Note:** On the `ci` channel, `runtimeVersion` / `aspNetCoreVersion` accept **only** a commit SHA (8–40 hex characters) or an empty value (= latest cached build). A feed version string is **rejected with an error** — version-string pinning is not supported on this channel. Conversely, on the other channels (`current` / `latest` / `edge`) those same arguments accept a version string and do **not** accept a commit SHA. The reported `runtimeVersion` / `aspNetCoreVersion` for a `ci` run are stamped with the resolved commit as `{feedVersion}+ci.{shortSha}`.

### Basic usage (latest cached build of both frameworks on main)

```
> crank --config benchmarks.yml --scenario json --profile aspnet-perf-lin --application.channel ci
```

### Bisecting ASP.NET Core (pin aspnetcore, runtime stays latest)

```
> crank --config benchmarks.yml --scenario json --profile aspnet-perf-lin --application.channel ci --application.aspNetCoreVersion a1b2c3d4e5f6...
```

### Bisecting the base runtime (pin runtime, aspnetcore stays latest)

```
> crank --config benchmarks.yml --scenario json --profile aspnet-perf-lin --application.channel ci --application.runtimeVersion a1b2c3d4e5f6...
```

### Pinning both

```
> crank --config benchmarks.yml --scenario json --profile aspnet-perf-lin --application.channel ci \
    --application.runtimeVersion 1111aaaa2222bbbb... \
    --application.aspNetCoreVersion 3333cccc4444dddd...
```

If a requested commit is not found in the cache, crank fails with an error rather than falling back.

### `ci` channel properties

| Property | Default | Description |
|----------|---------|-------------|
| `runtimeVersion` | (empty = latest) | On the `ci` channel: a [dotnet/runtime](https://github.com/dotnet/runtime) commit SHA (8–40 hex chars) to resolve from BCS and overlay onto `Microsoft.NETCore.App`. Empty uses the latest cached runtime build on `main`. A feed version string is rejected with an error. |
| `aspNetCoreVersion` | (empty = latest) | On the `ci` channel: a [dotnet/aspnetcore](https://github.com/dotnet/aspnetcore) commit SHA (8–40 hex chars) to resolve from BCS and place as `Microsoft.AspNetCore.App`. Empty uses the latest cached aspnetcore build on `main`. A feed version string is rejected with an error. |

The BCS configuration key (e.g., `coreclr_x64_linux` for runtime, `aspnetcore_x64_linux` for aspnetcore) is auto-detected per repo from the agent platform. Platforms with no aspnetcore config (there is no macOS/musl/arm32 in v1) fail loud rather than silently skipping.

### Agent configuration

The agent supports these command-line options for BCS:

| Option | Default | Description |
|--------|---------|-------------|
| `--build-cache-base-url` | `https://pvscmdupload.z22.web.core.windows.net` | Base URL for BCS blob storage. |
| `--build-cache-disabled` | (not set) | Disables BCS integration on this agent. |
