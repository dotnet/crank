# ASP.NET Benchmark Driver

## Usage

```
Usage: BenchmarksDriver [options]

Options:
  -?|-h|--help                                          Show help information

  These options are not specific to a Job

  --config <filename>                                            Configuration file or url. Can be used multiple times.
  --scenario <scenario>                                          Scenario to execute.
  --profile <profile>                                            Profiles to apply. Can be used multiple times.
  --variable <key=value>                                         A global variable. can be used multiple times.
  --compare <filename>                                           An optional filename to compare the results to. Can be used multiple times.
  
  --session                                                      A logical identifier to group related jobs.
  --description                                                  A string describing the job.
  --property <key=value>                                         Some custom key/value that will be added to the results, .e.g. --property arch=arm --property os=linux
  
  --sql                                                          Connection string or environment variable name of the SQL Server Database to store results in.
  --table                                                        Table or environment variable name of the SQL Database to store results in.
  --output <filename>                                            An optional filename to store the output document.
  --no-metadata                                                  Don't record metadata in the stored document.
  --no-measurements                                              Don't record measurements in the stored document.

  -i|--iterations <n>                                            The number of times to repeat the jobs to get average results. By default the last job only is repeated, unless '--repeat' is set. 
  -di|--display-iterations                                       Displays intermediate iterations results.
  --repeat <job>                                                 The job to repeat using the '--duration' or '--iterations' argument.
  --span <HH:mm:ss>                                              The duration while the job is repeated.
  --auto-flush                                                   Runs a single long-running job and flushes measurements automatically.
  -x|--exclude                                                   Excludes the specified number of high and low results, e.g., 1.
  -xo|--exclude-order                                            The result to use to detect the high and low results, e.g., 'load:wrk/rps/mean'", CommandOptionType.SingleValue);

  --chart                                                        Renders a chart for multi-value results.
  --chart-type [bar (default) | hex]                             Type of chart to render. Values are 'bar' (default) or 'hex'
  --chart-scale [off (default)| auto]                            Scale for chart. Values are 'off' (default) or 'auto'. When scale is off, the min value starts at 0.
  --script [name]                                                Execute a named script available in the configuration files. Can be used multiple times.

  These options are specific to a Job service named [JOB]

  --[JOB].endpoints <url>                                                  An endpoint on which to deploy the job definition, e.g., http://asp-perf-lin:5001. Can be used multiple times.

  ## Sources location

  --[JOB].source.repository                                                The git repository containing the source code to build, e.g., https://github.com/aspnet/aspnetcore
  --[JOB].source.branchOrCommit                                            A branch name or commit hash, e.g., my/branch, my/branch#commit-hash
  --[JOB].source.initSubmodules                                            Whether to init submodules when a git repository is used, e.g., true
  --[JOB].source.localFolder                                               The local path containing the source code to upload to the server. e.g., /code/mybenchmarks

  ## .NET options

  --[JOB].source.project <filename.csproj>                                 The project file to build, relative to the source code base path, e.g., src/Benchmarks/Benchmarks.csproj
  --[JOB].sdkVersion <version>                                             The version of the .NET SDK to install and use. By default the latest available build is used.
  --[JOB].runtimeVersion <version>                                         The version of the .NET runtime to install and use. It is defined as MicrosoftNETCoreAppPackageVersion in the build arguments. By default the latest available build is used. Setting this value forces the app to be deployed as stand-alone.
  --[JOB].aspNetCoreVersion <version>                                      The version of the ASP.NET runtime to install and use. It is defined as MicrosoftAspNetCoreAppPackageVersion in the build arguments. By default the latest available build is used.  Setting this value forces the app to be deployed as stand-alone.
  --[JOB].noGlobalJson <true|false>                                        Whether to not emit any global.json file to force the .NET SDK version to use. Default is false, meaning whatever version of the .NET SDK is chosen, it will be set in a global.json file.
  --[JOB].framework <tfm>                                                  The framework version to use in case it can't be assumed from the .NET runtime version. e.g., netcoreapp3.1
  --[JOB].buildArguments <argument>                                        An argument to pass to msbuild. Can be used multiple times to define multiple values.
  --[JOB].selfContained <true|false>                                       Whether to deploy the app as stand-alone. Default is false. Is is forced to 'true' if either runtimeVersion or aspnetVersion is defined as the SDK versions would be used otherwise.
  --[JOB].useMonoRuntime <jit|llvm-jit|llvm-aot>                           Use a specific mono runtime instead of the dotnet core runtime.
  
  ## Docker options

  --[JOB].source.dockerFile                                                The local path to the Docker file, e.g., frameworks/Rust/actix/actix-raw.dockerfile
  --[JOB].source.dockerImageName                                           The name of the docker image to create, e.g., actix_raw
  --[JOB].source.dockerContextDirectory                                    The folder in which the Docker file is built relative to, e.g., frameworks/Rust/actix/
  --[JOB].source.dockerFetchPath                                           The path in the Docker container that contains the base path for the --fetch option, e.g., ./output
  --[JOB].source.dockerLoad                                                The path of an image to use for 'docker load', e.g, "./myimage.tar"
  --[JOB].source.dockerCommand                                             The 'docker run' command, e.g, "./startup.sh"
  --[JOB].buildArguments <argument>                                        An argument to pass to 'docker build' as a '--build-arg' value. Can be used multiple times to define multiple values.

  ## Diagnostics

  --[JOB].dotnetTrace <true|false>                                         Whether to collect a diagnostics trace using dotnet-trace. An optional profile name or list of dotnet-trace providers can be passed. e.g., true
  --[JOB].dotnetTraceProviders <profile|provider|clrevent>                 A comma-separated list of trace providers. By default the profile 'cpu-sampling' is used. See https://github.com/dotnet/diagnostics/blob/master/documentation/dotnet-trace-instructions.md for details. e.g., "Microsoft-DotNETCore-SampleProfiler, gc-verbose, JIT+Contention".
  --[JOB].options.traceOutput <filename>                                   The name of the trace file. Can be a file prefix (app will add *.DATE*.zip) , or a specific name and no DATE* will be added e.g., c:\traces\mytrace
  --[JOB].options.collectCounters <true|false>                             Whether to collect dotnet counters. If set and 'counterProviders' is not, System.Runtime will be used by default.
  --[JOB].options.counterProviders <provider>                              The name of a performance counter provider from which to collect. e.g., System.Runtime, Microsoft-AspNetCore-Server-Kestrel, Microsoft.AspNetCore.Hosting, Microsoft.AspNetCore.Http.Connections, Grpc.AspNetCore.Server, Grpc.Net.client, Npgsql
  --[JOB].collectStartup <true|false>                                      Whether to include the startup phase in the traces, i.e after the application is launched and before it is marked as ready. For a web application it means before it is ready to accept requests.
  --[JOB].collect <true|false>                                             Whether to collect native traces. Uses PerfView on Windows and Perf/PerfCollect on Linux.
  --[JOB].collectArguments <arguments>                                     Native traces arguments, default is "BufferSizeMB=1024;CircularMB=4096;TplEvents=None", other suggested values: "BufferSizeMB=1024;CircularMB=4096;ThreadTime", "BufferSizeMB=1024;CircularMB=4096;GcOnly"

  ## Environment

  --[JOB].environmentVariables <key=value>                                 An environment variable key/value pair to assign to the process. Can be used multiple times to define multiple values.
  --[JOB].options.requiredOperatingSystem <linux|windows|osx>              The operating system the job can only run on.
  --[JOB].options.requiredArchitecture <x64|arm64>                         The architecture the job can only run on.
  --[JOB].memoryLimitInBytes <bytes>                                       The amount of memory available for the process.
  --[JOB].cpuSet <cpu-set>                                                 The list of CPUs available for the process, e.g., "0", "0-3", "1,3-4"
  --[JOB].cpuLimitRatio <number>                                           The amount of CPU available for the process, e.g., "0.5". For a 12 cores machines this value would result in 50% out of 1200% total available CPU time.

  ## Debugging

  --[JOB].noClean <true|false>                                             Whether to keep the work folder on the server or not. Default is false, such that each job is cleaned once it's finished.
  --[JOB].options.fetch <true|false>                                       Whether the benchmark folder is downloaded. e.g., true. For Docker see '--[JOB].source.dockerFetchPath'
  --[JOB].options.fetchOutput <filename>                                   The name of the fetched archive. Can be a file prefix (app will add *.DATE*.zip) , or a specific name (end in *.zip) and no DATE* will be added e.g., c:\publishedapps\myApp
  --[JOB].options.displayOutput <true|false>                               Whether to download and display the standard output of the benchmark.
  --[JOB].options.displayBuild <true|false>                                Whether to download and display the standard output of the build step (works for .NET and Docker).

  ## Files
  --[JOB].options.buildFiles <filename>                                    Build files that will be copied before the application is built. Accepts globing patterns and recursive marker (**). Format is 'path[;destination]'. Path can be a URL. e.g., c:\images\mydockerimage.tar, c:\code\Program.cs. If provided, the destination needs to be a folder name, relative to the root source.
  --[JOB].options.outputFiles <filename>                                   Output files that will be copied in the final application folder. Accepts globing patterns and recursive marker (**). Format is 'path[;destination]'. Path can be a URL. e.g., c:\build\Microsoft.AspNetCore.Mvc.dll, c:\files\samples\picture.png;wwwroot\picture.png. If provided, the destination needs to be a folder name, relative to the root source.
  --[JOB].options.reuseSource <true|false>                                  Reuse local or remote sources across benchmarks for the same source.
  --[JOB].options.reuseBuild <true|false>                                   Reuse build files across benchmarks. Don't use with floating runtime versions.

  ## Timeouts

  --[JOB].timeout                                                           Maximum duration in seconds of the job in seconds. Defaults to 0 (unlimited).
  --[JOB].buildTimeout                                                      Maximum duration of the build phase. Defaults to 00:10:00 (10 minutes).
  --[JOB].startTimeout                                                      Maximum duration of the start phase. Defaults to 00:03:00 (3 minutes).
  --[JOB].collectTimeout                                                    Maximum duration of the collect phase. Defaults to 00:05:00 (5 minutes).

  ## Telemetry

  --[JOB].options.discardResults <true|false>                              Whether to discard all the results from this job, for instance during a warmup job.

  # Example

  dotnet run --
    --config ..\..\..\benchmarks.compose.json 
    --scenario plaintext 

    --application.endpoints http://asp-perf-lin:5001
    --application.sdkversion 5.0.100-alpha1-015721 
    --application.dotnetTrace true 
    --application.options.collectCounters true

    --load.endpoints http://asp-perf-win:5001 
    --load.source.localFolder ..\..\..\..\PipeliningClient\ 
    --load.source.project PipeliningClient.csproj 
    --load.variables.warmup 0 
    --load.variables.duration 5  
    --variables preset-headers=none 
