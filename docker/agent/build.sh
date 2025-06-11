#!/usr/bin/env bash

cpuname=$(uname -p)
dockerfile="Dockerfile"

while [ $# -ne 0 ]
do
    case "$1" in
        --dockerfile)
            shift
            dockerfile="$1"
            shift
            ;;
        *)
            echo "Unknown option: $1"
            echo "Usage: $0 [--dockerfile <path>]"
            exit 1
            ;;
    esac
done

docker build -t crank-agent --build-arg CPUNAME=$cpuname -f "$dockerfile" ../../