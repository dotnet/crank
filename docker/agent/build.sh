#!/usr/bin/env bash

cpuname=$(uname -p)
dockerfile=${1:-Dockerfile} # Default to 'Dockerfile' if no argument is passed

docker build -t crank-agent --build-arg CPUNAME=$cpuname -f $dockerfile ../../