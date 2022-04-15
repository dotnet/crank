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
  --hardware             Hardware descriptor. Optional.
  --dotnethome           Folder to reuse for sdk and runtime installs.
  --relay                Connection string or environment variable name of the Azure Relay
                         Hybrid Connection to listen to. e.g.,
                         Endpoint=sb://mynamespace.servicebus.windows.net;...
  --relay-path           The hybrid connection name used to bind this agent. If not set the
                         --relay argument must contain 'EntityPath={name}'
  --relay-enable-http    Activates the HTTP port even if Azure Relay is used.
  --hardware-version     Hardware version (e.g, D3V2, Z420, ...). Optional.
  --no-cleanup           Don't kill processes or delete temp directories.
  --build-path           The path where applications are built.
  --build-timeout        Maximum duration of build task in minutes. Default 10 minutes.
  --service              Enables Crank.Agent to run as a windows service
```

## Running Crank.Agent as a service

At the moment, only Windows service is supported.

Deploy the agent as a dotnet tool (or published), and register the windows service with

```
dotnet tool install -g Microsoft.Crank.Agent --version "0.2.0-*" 
sc.exe create "CrankAgentService" binpath= "%USERPROFILE%\crank-agent.exe --url http://*:5001 --service"
```

You also should add the `--dotnethome` and `--build-path` parameters, in order to reuse sdk, and have a dedicated workspace for building and compiling.

The windows service must run with an account having rights on all the folders.
You may also need to allow the exposed port on your Windows firewall.

## Removing the service

To delete the service, use these commands:

```
sc.exe stop "CrankAgentService"
sc.exe delete "CrankAgentService"
```
