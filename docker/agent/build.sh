#!/usr/bin/env bash

cpuname=$(uname -p)
docker build -t crank-agent --build-arg CPUNAME=$cpuname -f Dockerfile ../../