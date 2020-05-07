New-Item -ItemType Directory -Force -Path .\.build
wget "https://raw.githubusercontent.com/dotnet/cli/master/scripts/obtain/dotnet-install.ps1" -OutFile ".\.build\dotnet-install.ps1"
.\.build\dotnet-install.ps1 -Channel 3.1.302 -SkipNonVersionedFiles -Version 32.1.302
dotnet build -c Release