#Requires -Version 7.2
# Licensed to the .NET Foundation under one or more agreements.
# The .NET Foundation licenses this file to you under the MIT license.
# See the LICENSE file in the project root for more information.

[CmdletBinding()]
param(
    [string] $BundlePath = $PSScriptRoot,
    [string] $WorkPath,
    [string] $Url = 'http://127.0.0.1:5010',
    [string] $RelayEnvironmentVariable,
    [switch] $EnableHttp,
    [string] $ManagedIdentityClientId
)

$ErrorActionPreference = 'Stop'
$BundlePath = (Resolve-Path -LiteralPath $BundlePath).Path
$agent = Join-Path $BundlePath 'agent' 'crank-agent.dll'
if (!(Test-Path -LiteralPath $agent)) { throw "Published agent not found: $agent" }
if (!$WorkPath) { $WorkPath = Join-Path $BundlePath ('agent-run-' + [Guid]::NewGuid().ToString('N')) }
if (Test-Path -LiteralPath $WorkPath) { throw 'WorkPath must be new; shared agent directories are not allowed.' }
$WorkPath = [IO.Path]::GetFullPath($WorkPath)
foreach ($name in @('', 'build', 'dotnet', 'temp', 'logs')) {
    $null = New-Item -ItemType Directory -Path (Join-Path $WorkPath $name)
}
$arguments = @($agent, '--no-cleanup', '--build-path', (Join-Path $WorkPath 'build'),
    '--dotnethome', (Join-Path $WorkPath 'dotnet'), '--log-path', (Join-Path $WorkPath 'logs'), '--url', $Url)
if ($RelayEnvironmentVariable) {
    if (![Environment]::GetEnvironmentVariable($RelayEnvironmentVariable)) {
        throw "Set the listen connection string in environment variable $RelayEnvironmentVariable first."
    }
    $arguments += @('--relay', $RelayEnvironmentVariable)
    if ($EnableHttp) { $arguments += '--relay-enable-http' }
}
elseif ($EnableHttp -or $ManagedIdentityClientId) {
    throw 'EnableHttp and ManagedIdentityClientId require RelayEnvironmentVariable.'
}
if ($ManagedIdentityClientId) { $arguments += @('--mi-client-id', $ManagedIdentityClientId) }
$oldTemp, $oldTmp, $oldTmpDir = $env:TEMP, $env:TMP, $env:TMPDIR
try {
    $env:TEMP = $env:TMP = $env:TMPDIR = Join-Path $WorkPath 'temp'
    Write-Host "Dedicated agent workspace: $WorkPath"
    Write-Host 'Use only a separate canary entity. Stop this foreground agent with Ctrl+C when finished.'
    & dotnet @arguments
    if ($LASTEXITCODE -ne 0) { throw "Agent exited with code $LASTEXITCODE. See $WorkPath" }
}
finally {
    $env:TEMP, $env:TMP, $env:TMPDIR = $oldTemp, $oldTmp, $oldTmpDir
}
