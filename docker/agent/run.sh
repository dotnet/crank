#!/usr/bin/env bash

url="http://*:5001"
name="crank-agent"
others=""

while [ $# -ne 0 ]
do
    case "$1" in
        --url)
            shift
            url="$1"
            shift
            ;;
        --name)
            shift
            name="$1"
            shift
            ;;
        *)
            others+=" $1"
            shift
            ;;
    esac
done

# cgroupfs is mapped to allow docker to create cgroups without permissions issues (cgroup v2)
# docker.sock is mapped to be able to manage other docker instances from this one
docker run -it --name $name -d --network host --restart always \
    --log-opt max-size=1G --privileged \
    -v /sys/fs/cgroup/:/sys/fs/cgroup/ \
    -v /var/run/docker.sock:/var/run/docker.sock \
    crank-agent \
    --url $url $others
