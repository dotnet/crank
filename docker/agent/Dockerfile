FROM mcr.microsoft.com/dotnet/core/sdk:3.1 as build-env

COPY . .

ENV DOTNET_SKIP_FIRST_TIME_EXPERIENCE 1
ENV DOTNET_CLI_TELEMETRY_OPTOUT 1

# Build self contained

RUN dotnet publish -c Release src/Microsoft.Crank.Agent -r linux-x64 --output /app

# Build runtime image
FROM mcr.microsoft.com/dotnet/core/runtime-deps:3.1

COPY --from=build-env /app /app

# Install dependencies
RUN apt-get update \
    && apt-get install -y --no-install-recommends \
        git \
        procps \
        cgroup-bin

# Install perfcollect
RUN apt-get update \
    && apt-get install -y --no-install-recommends \
        zip \
        linux-perf \
        lttng-tools \
        lttng-modules-dkms \
        liblttng-ust-dev \
        binutils

RUN cp /usr/bin/perf_4.19 /usr/bin/perf

# Downloading perfcollect - This branch contains SIGINT fixes
ADD https://raw.githubusercontent.com/sebastienros/perfview/sebros/sigint/src/perfcollect/perfcollect /usr/bin/perfcollect

RUN chmod +x /usr/bin/perfcollect


ENTRYPOINT [ "/app/crank-agent", "--url", "http://*:5001" ]
