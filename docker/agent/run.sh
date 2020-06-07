#!/usr/bin/env bash

docker run -it --name crank-agent -d --network host --restart always --privileged crank-agent "$@"
