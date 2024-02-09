## Description

This tutorial explains how you can define commands that should be run on the local machine prior to a job being sent to the agent.

## Motivation and Use Cases

Crank is capable of building your project on the agent itself, and this is the recommended approach if it is acceptable for you as it means you don't have to worry about cross-platform build concerns. However, sometimes the build itself may take a long time or the job requires uploading a large amount of source files to the agent which may make crank not practical to use. 

With pre-commands, you can define commands to be run on the local machine prior to the job being sent to the agent, Using this, you can build the project first, then upload only the built project to crank instead. This could also be useful if you just want to minimise the amount of data being uploaded to the agent and want to write a pre-command that organises all the data you want into a local folder and using that local folder as the source.

## Example

The file at `/crank/samples/precommands/precommands.benchmarks.yml` demonstrates how to use this.

```yaml
commands:
  # Assumes that the crank repository is the working directory 
  buildHello:
    - condition: job.environment.platform == "windows"
      scriptType: batch
      script: dotnet build -c Release -f net8.0 .\samples\hello\hello.csproj
    - condition: job.environment.platform != "windows"
      scriptType: bash
      script: dotnet build -c Release -f net8.0 ./samples/hello/hello.csproj

jobs:
  server:
    source:
      localFolder: ../../artifacts/bin/hello/Release/net8.0
    executable: dotnet
    arguments: hello.dll
    noBuild: true
    beforeJob:
      - buildHello
    readyStateText: Application started.
```

In this example, we define a `buildHello` command which will build `hello.csproj` locally depending on the current platform. The `beforeJob` property defines the order of the precommands to be run. The source points to a `localFolder` which is the path to where the built app will be relative to the yaml file. The `executable` and `arguments` properties indicate that the job should run `dotnet hello.dll`, and `noBuild` is set to true to tell the agent that there is no build needed.

## More details

Below is all the options you can set on a command definition:

```yaml
condition: bool # Defaults to "true". A javascript expression which if evaluated to true, will run the definition.
script: str # The full script to run, can contain multiple lines.
filePath: str # A path to a script file.
scriptType: powershell | bash | batch # The type of script found in the script property or at the filePath.
continueOnError: bool # Defaults to false. If false, will prevent the job from being sent to the agent if the precommand returns an unsuccessful exit code.
successExitCodes: [int] # Defaults to [0]. A list of exit codes to be treated as successful in conjunction with continueOnError.
```

The `condition` property will have access to the whole yaml configuration object via a global variable `configuration`, and access to the current job object via a global variable `job`. An easy way to get information about the local environment is through the `job.environment` property which has the following information:

```yaml
platform: windows | linux | osx | other
architecture: x86 | x64 | arm | arm64
```

`commands` can also be defined inside a `job` if you prefer. When defining a command, you are also able to take advantage of the variable substitution like the rest of the yaml. An example of this is below:

```yaml
jobs:
  server:
    variables:
      configuration: Release
      framework: net8.0
      rid: win-x64
    commands:
      publishHello:
        - condition: job.environment.platform == "windows"
          scriptType: batch
          script: dotnet publish -c {{ configuration }} -f {{ framework }} -r {{ rid }} .\samples\hello\hello.csproj
    beforeJob:
      - publishHello
```

In addition to `beforeJob`, you can also specify `afterJob` to run commands locally after the jobs have completed to run any cleanup commands.
