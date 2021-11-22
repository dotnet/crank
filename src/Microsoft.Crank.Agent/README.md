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
```
