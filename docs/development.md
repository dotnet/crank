## Running from source

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
