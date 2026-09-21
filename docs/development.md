## Running from source

Install the .NET 10 SDK version specified in `global.json` (or a newer .NET 10 SDK).

To run the CI restore, build, test, and package steps locally with warnings treated as errors:

```sh
bash eng/common/build.sh --restore --build --test --pack --ci --configuration Release
```

The build script also runs on macOS, where platform-specific integration tests are
skipped. This command does not sign or publish artifacts. Do not add `--prepareMachine`
on a shared development machine: it terminates build processes.

Crank can be started from its source code by executing these commands:

### Crank Agent

```
cd ./src/Microsoft.Crank.Agent
dotnet run
```

### Crank Controller

```
cd ./src/Microsoft.Crank.Controller
dotnet run  --config ../../samples/hello/hello.benchmarks.yml --scenario hello --profile local
```
