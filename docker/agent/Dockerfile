FROM mcr.microsoft.com/dotnet/core/sdk:3.1

COPY . .

RUN dotnet build -c Release

ENTRYPOINT ["dotnet", "artifacts/bin/Microsoft.Crank.Agent/Release/netcoreapp3.1/crank-agent.dll"]
