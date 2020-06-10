FROM mcr.microsoft.com/dotnet/core/sdk:3.1 as build-env

COPY . .

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

ENTRYPOINT [ "/app/crank-agent" ]
