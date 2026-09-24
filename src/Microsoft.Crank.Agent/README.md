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

## Azure Relay hosting

The agent uses a Crank-owned ASP.NET Core `IServer` adapter over
`Microsoft.Azure.Relay` 3.1.1. It no longer loads the archived
`Microsoft.Azure.Relay.AspNetCore` package. Publish/redeploy the complete agent;
replacing individual SDK DLLs is not a supported upgrade.

| Options | Transport | Address exported to benchmark jobs |
| --- | --- | --- |
| No `--relay` | Kestrel HTTP, unchanged | Bound HTTP address (`--url`) |
| `--relay` | Relay only; no local listening socket or loopback proxy | Public `https://<namespace>/<entity>` |
| `--relay --relay-enable-http` | Relay and Kestrel, sharing controllers and the job repository | Bound HTTP address (`--url`) |

The SDK listener uses `sb://`; controllers and the `CRANK_AGENT_URL` /
`CRANK_JOB_LOCAL_URL` job variables use the addresses above. Wildcard HTTP hosts
are still normalized to loopback for those variables. The job processor starts
only after all required listeners start successfully. A failed Relay startup
rolls back the HTTP listener in combined mode.

Existing flags and authentication are unchanged: `--relay` accepts a connection
string or its environment-variable name, and `--relay-path` overrides
`EntityPath`. Explicit managed-identity credentials take precedence over
certificate credentials, then connection-string authentication is used. Token
acquisition/renewal, reconnects, control WebSockets and rendezvous connections
remain SDK responsibilities. Never include connection strings or tokens in
diagnostic reports.

The adapter preserves the entity prefix as `Request.PathBase`. Controller job
`Location` headers remain `/jobs/{id}`; the Crank controller combines those with
the entity endpoint. Request bodies do not require `Content-Length`, and gzip
uploads are passed through unchanged for the existing controllers to decompress.
The upload decompressor stays alive until the asynchronous transfer completes.
Uploads and downloads are streamed, not buffered in full. Endpoint request-size
limits still apply (including the 10,000,000,000-byte upload limits); the default
is 10 GiB. Requests run independently so an upload does not serialize `/touch`.

### Transport limitations and shutdown

- This is an HTTP adapter, not an upgrade/WebSocket server. SDK connection
  upgrade requests are explicitly rejected. HTTP trailers are not exposed.
- Relay removes transport headers, including the original `Host`,
  `Content-Length`, `Transfer-Encoding`, and hop-by-hop fields. The adapter uses
  the public Relay host and SDK body-presence information; it cannot recover
  original wire headers. Application authorization/custom headers are preserved
  when delivered by the service.
- The SDK's 64 KiB control-channel threshold is **not** a maximum upload or
  download size. Larger bodies use rendezvous. `FlushAsync` and
  `DisableBuffering` cannot force the SDK to flush: small responses may remain
  buffered until close, overflow or the SDK's approximately two-second timer.
  `HasStarted` means application headers have been committed, not that the client
  has already received them. `Response.CompleteAsync()` awaits SDK response close;
  completion callbacks and registered resource disposal still wait for the
  application to return.
- `RequestAborted` observes shutdown and detected stream I/O failures. The SDK
  exposes no direct per-request client-disconnect token. A disconnected client
  may not be detected until an I/O operation fails. Service response/idle limits
  are not a blanket total-transfer timeout.
- Startup waits are bounded by the connection string's `OperationTimeout` and
  caller cancellation, including when SDK token acquisition ignores its token.
  Shutdown does not wait indefinitely for startup. Late SDK open/close operations
  remain observed, with their cancellation resources retained until completion;
  a late open cannot reactivate request admission or publish an address.
- Stop rejects new HTTP work with a completed 503 response, drains admitted
  applications for up to 30 seconds (or the host's earlier cancellation), then
  cancels requests and closes the listener. Listener close has a ten-second
  budget; cancelled requests have a further five-second completion grace period.
  Public SDK response `CloseAsync()` has no cancellation parameter. An
  application ignoring cancellation or an outstanding response close can outlive
  these waits; the agent logs this and continues observing cleanup rather than
  claiming to have forcibly terminated it.

### Validation

Deterministic tests exercise the production registration and owned server's
startup with a fake SDK boundary, real Kestrel in combined mode, and the published
Relay SDK with local token callbacks and in-memory WebSockets. They do not prove
Azure service compatibility:

```powershell
dotnet test test\Microsoft.Crank.UnitTests\Microsoft.Crank.UnitTests.csproj --filter "FullyQualifiedName~Relay|FullyQualifiedName~CompositeServerTests"
dotnet publish src\Microsoft.Crank.Agent\Microsoft.Crank.Agent.csproj --framework net10.0 --configuration Release
```

**Opt-in live acceptance (requires an authorized, separate canary entity):**

1. Provisioning resources and supplying credentials are operator actions, not
   part of the test command. Use separate listen/send credentials and an unused
   canary hybrid connection. Never run the old and new agents on the same
   production entity: Relay balances among listeners without job affinity, while
   job state is in memory.
2. Start the published agent with `--relay CRANK_RELAY_LISTEN --no-cleanup`,
   dedicated `--build-path` and `--dotnethome` directories, and then repeat with
   `--relay-enable-http --url http://127.0.0.1:<dedicated-port>`. Confirm Relay-only
   has no listening HTTP socket and that the job URL variables match the table.
   Also run an HTTP-only control. Do not use shared temporary workspaces or
   machine-wide process cleanup.
3. Point an existing canary benchmark configuration at
   `https://<namespace>.servicebus.windows.net/<entity>` and run the controller
   with `--relay CRANK_RELAY_SEND`. Exercise JSON job creation and its Location
   follow-up, source/build/attachment uploads (raw and gzip with unknown length),
   run, stop, download/fetch/trace/dump, custom headers and temporary-file cleanup.
   Include payloads of 65,535, 65,536, 65,537 bytes and a representative large
   artifact. Compare content hashes and gzip metadata, not just status codes.
4. Transfer concurrently while checking the controller's two-second `/touch`
   cadence and five-second request deadline. Disconnect a sender during upload
   and download, and stop the agent during active work. Record observed timeout,
   cancellation, cleanup and memory behavior. Do not send 10 GB merely to test
   an endpoint limit; use the deterministic boundary tests for that.
5. Repeat with the authorized managed-identity/certificate credentials (and
   controller Entra/Azure CLI authentication where used). Keep the canary alive
   through token renewal, interrupt/recover network connectivity, and confirm
   requests resume without restarting the job repository.
6. Before rollout, smoke-test the published Windows service and the intended
   Docker image under operator supervision. Existing certificate mounts and
   networking flags are unchanged. Do not launch `docker/agent/run.sh` just for
   testing on a shared machine: it is privileged, mounts the Docker socket, and
   installs an always-restarting container.

Record live results separately from unit results, including which authentication
mode and SDK version were tested. Missing credentials, unavailable container
infrastructure or NuGet connectivity are unverified/environmental outcomes, not
successful service acceptance or proof of an adapter defect.

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
