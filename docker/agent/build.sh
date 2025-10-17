#!/usr/bin/env bash

cpuname=$(uname -p)
dockerfile="Dockerfile"
enable_fips="false"

while [ $# -ne 0 ]
do
    case "$1" in
        --dockerfile)
            shift
            dockerfile="$1"
            shift
            ;;
        --enable-fips)
            enable_fips="true"
            shift
            ;;
        *)
            echo "Unknown option: $1"
            echo "Usage: $0 [--dockerfile <path>] [--enable-fips]"
            exit 1
            ;;
    esac
done

docker build -t crank-agent --build-arg CPUNAME=$cpuname --build-arg ENABLE_FIPS_MODE=$enable_fips -f "$dockerfile" ../../