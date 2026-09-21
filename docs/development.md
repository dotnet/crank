## Running from source

Install the .NET 10 SDK version specified in `global.json` (or a newer .NET 10 SDK).

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
