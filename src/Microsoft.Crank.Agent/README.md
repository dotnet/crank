# Crank Benchmarks Agent

## Usage

```
Usage: crank-agent [options]

Options:
  -?|-h|--help           Show help information
  -u|--url               URL for Rest APIs. Default is 'http://*:5010'.
  -n|--hostname          Hostname for benchmark server. Default is 'SEBROS-SURLAP3'.
  -nd|--docker-hostname  Hostname for benchmark server when running Docker on a different
                         hostname.
  --hardware             Hardware (Cloud or Physical).
  --dotnethome           Folder to reuse for sdk and runtime installs.
  --relay                Connection string or environment variable name of the Azure Relay
                         Hybrid Connection to listen to. e.g.,
                         Endpoint=sb://mynamespace.servicebus.windows.net;...
  --relay-path           The hybrid connection name used to bind this agent. If not set the
                         --relay argument must contain 'EntityPath={name}'
  --relay-enable-http    Activates the HTTP port even if Azure Relay is used.
  --hardware-version     Hardware version (e.g, D3V2, Z420, ...).
  --no-cleanup           Don't kill processes or delete temp directories.
  --build-path           The path where applications are built.
  --build-timeout        Maximum duration of build task in minutes. Default 10 minutes.
  --service              Enables Crank.Agent to run as a windows service
```

## Running Crank.Agent as a service

At the moment, only windows service is supported.

Deploy the agent as a dotnet tool (or published), and register the windows service with

```
SC CREATE "CrankAgentService" binpath= "X:\abosulte_path\crank-agent.exe --url http://exposed-URL:5010 --service"
```

You also should add the `--dotnethome` and `--build-path` parameters, in order to reuse sdk, and have a dedicated workspace for building and compiling.

The windows service must run with an account having rights on all the folders.
You also need to allow the exposed port on your windows firewall.
