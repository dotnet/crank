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
To stop the container, run `./stop.sh`

### Displaying the agent log

To display the live log, run the following command:

```
docker logs -f --tail 100 crank-agent
```

### Continuous integration

In order to restart and update the agent regularly, the following cron job can be used.

- Edit the crontab file:

```
crontab -e
```

- Add this entry:

```
0 0 * * * cd [PATH_TO_CRANK]/src/crank/docker/agent; ./stop.sh; docker rm -f $(docker ps -a -q --filter "label=benchmarks"); docker system prune --all --force --volumes; git checkout -f master; git pull; ./build.sh; ./run.sh
```

This will stop any running agent, clean all docker images used to run benchmarks, update the GitHub repositor, build and restart the agent image.
