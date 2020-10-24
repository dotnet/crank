#!/usr/bin/env bash

docker run -it --name crank-agent -d --network host --restart always --privileged -v /var/run/docker.sock:/var/run/docker.sock crank-agent "$@"
