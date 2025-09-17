FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build-env

COPY . .

ENV DOTNET_SKIP_FIRST_TIME_EXPERIENCE=1
ENV DOTNET_CLI_TELEMETRY_OPTOUT=1

# Build self contained

RUN dotnet publish -c Release src/Microsoft.Crank.Agent --output /app --framework net8.0

# Build runtime image
# FROM mcr.microsoft.com/dotnet/aspnet:8.0
# Use SDK image as it is required for the dotnet tools
FROM mcr.microsoft.com/dotnet/sdk:8.0

ARG CPUNAME=x86_64

# Install dotnet-symbols
RUN dotnet tool install -g dotnet-symbol
ENV PATH="${PATH}:/root/.dotnet/tools"

# Install dependencies
RUN apt-get update \
    && apt-get install -y --no-install-recommends \
        git \
        procps \
        cgroup-tools \
        curl \
        wget \
        # dotnet performance repo microbenchmark dependencies
        libgdiplus \
        # libmsquic requirements
        gnupg2 \
        software-properties-common \
        # NativeAOT requirements
        clang \
        zlib1g-dev \
        libkrb5-dev \
        # .NET 9.0 requirement
        libc6 

# Install HTTP/3 support
RUN curl -LO https://packages.microsoft.com/keys/microsoft.asc && \
    echo 2fa9c05d591a1582a9aba276272478c262e95ad00acf60eaee1644d93941e3c6 microsoft.asc| sha256sum --check - && \
    apt-key add microsoft.asc && \
    rm microsoft.asc && \
    echo deb https://packages.microsoft.com/debian/12/prod bookworm main >> /etc/apt/sources.list.d/microsoft.list && \
    apt-get update && \
    apt-get install -y libmsquic && \
    rm -rf /var/lib/apt/lists/*

# Build and install h2load. Required as there isn't a way to distribute h2load as a single file to download
RUN apt-get update \
    && apt-get install -y --no-install-recommends \
        g++ make binutils autoconf automake autotools-dev libtool pkg-config \
        zlib1g-dev libcunit1-dev libxml2-dev libev-dev libevent-dev libjansson-dev \
        libc-ares-dev libjemalloc-dev libsystemd-dev \
        python-is-python3 python3-dev python3-setuptools

ENV DEBIAN_FRONTEND=noninteractive
# Add the Debian sid repository
RUN echo 'deb http://deb.debian.org/debian sid main' >> /etc/apt/sources.list \
    && echo 'deb http://deb.debian.org/debian-debug sid-debug main' >> /etc/apt/sources.list

ENV OPENSSL_VERSION=3.5.*
# Update package lists and install specific versions of OpenSSL, libssl-dev, and their debug symbols
RUN apt-get update \
    && apt-get install -y --no-install-recommends \
        openssl=${OPENSSL_VERSION} libssl-dev=${OPENSSL_VERSION} openssl-dbgsym=${OPENSSL_VERSION} libssl3t64-dbgsym=${OPENSSL_VERSION} \
    && apt-get clean

# If nghttp2 build fail just ignore it
ENV NGHTTP2_VERSION=1.58.0
RUN cd /tmp \
    && curl -L "https://github.com/nghttp2/nghttp2/releases/download/v${NGHTTP2_VERSION}/nghttp2-${NGHTTP2_VERSION}.tar.gz" -o "nghttp2-${NGHTTP2_VERSION}.tar.gz" \
    && tar -zxvf "nghttp2-${NGHTTP2_VERSION}.tar.gz" \
    && cd /tmp/nghttp2-$NGHTTP2_VERSION \
    && ./configure \
    && make \
    && make install || true

# Install docker client
ENV DOCKER_VERSION=17.09.0-ce
RUN cd /tmp \
    && curl "https://download.docker.com/linux/static/stable/${CPUNAME}/docker-${DOCKER_VERSION}.tgz" -o docker.tgz \
    && tar xvzf docker.tgz \
    && cp docker/docker /usr/bin \
    && rm -rf docker.tgz docker

# Install Azure CLI - https://learn.microsoft.com/en-us/cli/azure/install-azure-cli-linux?pivots=apt#option-2-step-by-step-installation-instructions
# We cannot use the helper script to install the CLI with one command as the script
# gets an amd64-only DEB package while this image needs to support both amd64 and arm64.
RUN apt-get update \
    && apt-get install -y \
        apt-transport-https \
        ca-certificates \
        lsb-release \
        gnupg \
    && mkdir -p /etc/apt/keyrings \
    && curl -sLS https://packages.microsoft.com/keys/microsoft.asc | gpg --dearmor | tee /etc/apt/keyrings/microsoft.gpg > /dev/null \ 
    && chmod go+r /etc/apt/keyrings/microsoft.gpg \
    && echo "deb [arch=$(dpkg --print-architecture) signed-by=/etc/apt/keyrings/microsoft.gpg] https://packages.microsoft.com/repos/azure-cli/ $(lsb_release -cs) main" | tee /etc/apt/sources.list.d/azure-cli.list \
    && apt-get update \
    && apt-get install -y azure-cli

# Install perfcollect
ADD https://raw.githubusercontent.com/microsoft/perfview/main/src/perfcollect/perfcollect /usr/bin/perfcollect
RUN chmod +x /usr/bin/perfcollect
RUN /usr/bin/perfcollect install

COPY --from=build-env /app /app

ENTRYPOINT [ "/app/crank-agent" ]
