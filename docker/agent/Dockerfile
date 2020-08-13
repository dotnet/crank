FROM mcr.microsoft.com/dotnet/core/sdk:3.1 as build-env

COPY . .

ENV DOTNET_SKIP_FIRST_TIME_EXPERIENCE 1
ENV DOTNET_CLI_TELEMETRY_OPTOUT 1

# Build self contained

RUN dotnet publish -c Release src/Microsoft.Crank.Agent --output /app

# Build runtime image
FROM mcr.microsoft.com/dotnet/core/aspnet:3.1

# Install dependencies
RUN apt-get update \
    && apt-get install -y --no-install-recommends \
        git \
        procps \
        cgroup-bin \
        curl

# Build and install h2load. Required as there isn't a way to distribute h2load as a single file to download
RUN apt-get update \
    && apt-get install -y --no-install-recommends \
        g++ make binutils autoconf automake autotools-dev libtool pkg-config \
        zlib1g-dev libcunit1-dev libssl-dev libxml2-dev libev-dev libevent-dev libjansson-dev \
        libc-ares-dev libjemalloc-dev libsystemd-dev \
        cython python3-dev python-setuptools

ENV NGHTTP2_VERSION 1.41.0
RUN cd /tmp \
    && curl -L "https://github.com/nghttp2/nghttp2/releases/download/v${NGHTTP2_VERSION}/nghttp2-${NGHTTP2_VERSION}.tar.gz" -o "nghttp2-${NGHTTP2_VERSION}.tar.gz" \
    && tar -zxvf "nghttp2-${NGHTTP2_VERSION}.tar.gz" \
    && cd /tmp/nghttp2-$NGHTTP2_VERSION \
    && ./configure \
    && make \
    && make intall    

# Install docker client
ENV DOCKER_VERSION 17.09.0-ce
RUN cd /tmp \
    && curl https://download.docker.com/linux/static/stable/x86_64/docker-$DOCKER_VERSION.tgz -o docker.tgz \
    && tar xvzf docker.tgz \
    && cp docker/docker /usr/bin \
    && rm -rf docker.tgz docker

# Install perfcollect
RUN apt-get update \
    && apt-get install -y --no-install-recommends \
        zip \
        unzip \
        linux-perf \
        lttng-tools \
        lttng-modules-dkms \
        liblttng-ust-dev \
        binutils

RUN cp /usr/bin/perf_4.19 /usr/bin/perf

# Downloading perfcollect - This branch contains SIGINT fixes
ADD https://raw.githubusercontent.com/sebastienros/perfview/sebros/sigint/src/perfcollect/perfcollect /usr/bin/perfcollect

RUN chmod +x /usr/bin/perfcollect

COPY --from=build-env /app /app

ENTRYPOINT [ "/app/crank-agent", "--url", "http://*:5001" ]
