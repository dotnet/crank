# Crank Pull Request Bot

## Usage

```
crank-pr:
  Crank Pull Requests Bot

Usage:
  crank-pr [options]

Options:
  --workspace <workspace>                The folder used to clone the repository. Defaut is temp folder.
  --benchmarks <benchmarks>              The benchmarks to run.
  --profiles <profiles>                  The profiles to run the benchmarks one.
  --components <components>              The components to build.
  --limit <limit>                        The maximum number of commands to execute. 0 for unlimited.
  --repository <repository>              The repository for which pull-request comments should be scanned, e.g., https://github.com/dotnet/aspnetcore,
                                         dotnet/aspnetcore
  --pull-request <pull-request>          The Pull Request url or id to benchmark, e.g., https://github.com/dotnet/aspnetcore/pull/39527, 39527
  --publish-results <true|false>         Publishes the results on the original PR.
  --access-token <access-token>          The GitHub account access token. (Secured)
  --app-key <app-key>                    The GitHub application key. (Secured)
  --app-id <app-id>                      The GitHub application id.
  --install-id <install-id>              The GitHub installation id.
  --github-base-url <github-base-url>    The GitHub base URL if using GitHub Enterprise, e.g., https://github.local
  --arguments "<first second third ...>" Any additional arguments to pass through to crank.
  --config <config> (REQUIRED)           The path to a configuration file.
  --version                              Show version information
  --age                                  The age of the most recent comment to look for in minutes. Default is 60.
  -?, -h, --help                         Show help and usage information
```

Example:

The following example runs `plaintext` benchmark on the `aspnet-perf-lin` environment after building `kestrel` with the specified pull-request.

```
dotnet run --config .\sample.config.yml --pull-request https://github.com/sebastienros/aspnetcore/pull/2 --benchmarks plaintext --profiles aspnet-perf-lin --components kestrel
```

The following command execute any pending `/benchmark` command provide in a pull-request comment of the aspnetcore repository.
The command format is `/benchmark <benchmarks[,...]> <profiles[,...]> <components,[...]>`

```
dotnet run --config .\sample.config.yml --repository https://github.com/sebastienros/aspnetcore --publish-results true
```
