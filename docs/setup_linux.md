## Setting up an agent on Linux

On Linux, it is recommended to setup the Agent using the Docker file provided in this repository.
There instructions are valid for x86_64 and ARM64 (aka ARMv8 or AARCH64).

### Installing Docker

- Install docker from the automated script

```
curl -sSL https://get.docker.com | sh
```

- Add the local account to the docker user group so that you can execute docker commands without sudo

```
sudo groupadd docker
sudo usermod -aG docker $USER
newgrp docker
```

- Reopen the session with the account
- Check Docker is running

```
docker run hello-world
```

### Starting the Agent

- Clone the `crank` repository

```
mkdir ~/src
cd ~/src
git clone https://github.com/dotnet/crank
```

- Build and run the Agent docker image

```
cd ~/src/crank/docker/agent
./build.sh
./run.sh
```

This will build the image with all the dependencies (perfcollect, ...) and start a container named `crank-agent`.
Optionally, a dockerfile can be passed in as an argument to `./build.sh` to use different base images (i.e. Dockerfile.AzureLinux3) via `--dockerfile <file path>`.
To stop the container, run `./stop.sh`

### Displaying the agent log

To display the live log, run the following command:

```
docker logs -f --tail 100 crank-agent
```

### Collecting traces on newer Linux distributions

When legacy native trace collection (`--[JOB].collect true`) fails, Crank reports the perfcollect error in the controller output. For example, `LTTng not installed` means the dependency is missing from the agent container, not necessarily that the host lacks perf support.

Legacy perfcollect's .NET runtime-event collection also depends on a compatible LTTng-UST library. [LTTng-UST 2.13 changed its userspace ABI](https://github.com/dotnet/runtime/issues/57784), including the library name from `liblttng-ust.so.0` to `liblttng-ust.so.1`, which prevents the affected .NET tracepoint provider from loading on distributions that only supply the newer library. This is a **userspace library ABI incompatibility**, not a Linux kernel ABI change. The [perfcollect installer](https://github.com/microsoft/perfview/blob/main/src/perfcollect/perfcollect) skips LTTng installation on Ubuntu 22 and newer because the available version is incompatible with .NET. Installing the newer LTTng packages alone therefore does not restore .NET runtime-event collection.

For .NET 10 and newer on compatible Linux hosts, the recommended approach is [dotnet-trace's `collect-linux` mode](https://learn.microsoft.com/dotnet/core/diagnostics/dotnet-trace#dotnet-trace-collect-linux). It collects native/kernel CPU samples and .NET runtime events without depending on LTTng:

```text
--[JOB].collect false --[JOB].dotnetTrace true --[JOB].dotnetTraceCollectMode collect-linux --[JOB].dotnetTraceProviders "cpu-sampling,dotnet-common"
```

Replace `[JOB]` with the scenario's job name, such as `client`, and remove the legacy `collectArguments` option. Use a current Crank controller and agent that support this mode. Collection requires Linux x64 or Arm64 with glibc 2.27 or newer, root in the agent environment, and a kernel >= 6.4 with `CONFIG_USER_EVENTS=y` and accessible mounted tracefs (normally `/sys/kernel/tracing`). Containers must also permit perf access and expose tracefs; the repository's `docker/agent/run.sh` configures a privileged container with the tracefs mount.

If the host cannot meet those requirements, use EventPipe collection (`dotnetTraceCollectMode collect` or `default`) instead. EventPipe does not provide the native/kernel CPU stack coverage of `collect-linux`.

### Continuous integration

In order to restart and update the agent regularly, the following cron job can be used.

- Edit the crontab file:

```
crontab -e
```

_build.sh uses arguments, so depending on the machine you could choose behavior building docker image:_
- Change dockerfile used: `--dockerfile ...`
- Change openssl.conf to FIPS compliant CipherSuties and ECs: `--enable-fips`

- Add this entry:

```
0 0 * * * cd [PATH_TO_CRANK]/src/crank/docker/agent; ./stop.sh; docker rm -f $(docker ps -a -q --filter "label=benchmarks"); docker system prune --all --force --volumes; git checkout -f master; git pull; ./build.sh; ./run.sh
```

This will stop any running agent, clean all docker images used to run benchmarks, update the GitHub repositor, build and restart the agent image.
