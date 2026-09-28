#Requires -Version 7.2
# Licensed to the .NET Foundation under one or more agreements.
# The .NET Foundation licenses this file to you under the MIT license.
# See the LICENSE file in the project root for more information.

[CmdletBinding()]
param(
    [string] $BundlePath,
    [switch] $PrepareOnly,
    [uri] $Endpoint,
    [switch] $Relay,
    [string] $RelayEnvironmentVariable,
    [string] $ExpectedAgentUrl,
    [string] $SdkVersion = '10.0.401',
    [string] $RuntimeVersion = '10.0.12',
    [ValidateRange(60, 3600)]
    [int] $TimeoutSeconds = 900
)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

function Start-LoggedProcess([string] $FileName, [string[]] $Arguments, [string] $Name, [string] $Directory) {
    $info = [Diagnostics.ProcessStartInfo]::new($FileName)
    $info.UseShellExecute = $false
    $info.RedirectStandardOutput = $info.RedirectStandardError = $true
    $info.WorkingDirectory = $Directory
    foreach ($argument in $Arguments) { $info.ArgumentList.Add($argument) }
    $process = [Diagnostics.Process]::new()
    $process.StartInfo = $info
    $stdout = [IO.File]::Create((Join-Path $Directory "$Name.stdout.log"))
    $stderr = [IO.File]::Create((Join-Path $Directory "$Name.stderr.log"))
    try {
        if (!$process.Start()) { throw "Unable to start $Name" }
        return @{
            Process = $process; Stdout = $stdout; Stderr = $stderr
            OutTask = $process.StandardOutput.BaseStream.CopyToAsync($stdout)
            ErrTask = $process.StandardError.BaseStream.CopyToAsync($stderr)
        }
    }
    catch {
        $stdout.Dispose()
        $stderr.Dispose()
        $process.Dispose()
        throw
    }
}

function Close-LoggedProcess($Command) {
    if ($null -eq $Command) { return }
    try {
        if (!$Command.Process.HasExited) {
            # Only the process tree started by this invocation, never processes by name.
            $Command.Process.Kill($true)
            $Command.Process.WaitForExit()
        }
        $null = $Command.OutTask.GetAwaiter().GetResult()
        $null = $Command.ErrTask.GetAwaiter().GetResult()
    }
    finally {
        $Command.Stdout.Dispose()
        $Command.Stderr.Dispose()
        $Command.Process.Dispose()
    }
}

function New-Payload([string] $Path, [int] $Size) {
    $random = [Random]::new($Size)
    $buffer = [byte[]]::new([Math]::Min($Size, 65536))
    $stream = [IO.File]::Create($Path)
    try {
        $remaining = $Size
        while ($remaining -gt 0) {
            $random.NextBytes($buffer)
            $count = [Math]::Min($remaining, $buffer.Length)
            $stream.Write($buffer, 0, $count)
            $remaining -= $count
        }
    }
    finally { $stream.Dispose() }
}

if ($Relay -and !$Endpoint) { throw 'Relay requires an explicit authorized canary Endpoint.' }
if ($RelayEnvironmentVariable -and !$Relay) { throw 'RelayEnvironmentVariable requires Relay.' }
if ($Relay -and $Endpoint.Scheme -ne 'https') { throw 'Relay Endpoint must be the public HTTPS entity URL.' }
if ($Endpoint -and ($Endpoint.Scheme -notin @('http', 'https') -or $Endpoint.UserInfo -or $Endpoint.Query -or $Endpoint.Fragment)) {
    throw 'Endpoint must be an HTTP(S) agent base URL without credentials, query, or fragment.'
}
if ($RelayEnvironmentVariable -and ![Environment]::GetEnvironmentVariable($RelayEnvironmentVariable)) {
    throw "Set the sender connection string in environment variable $RelayEnvironmentVariable first."
}
$dotnet = (Get-Command dotnet -CommandType Application).Source
if (!$BundlePath) {
    $repo = (Resolve-Path (Join-Path $PSScriptRoot '..' '..')).Path
    $BundlePath = Join-Path $repo 'artifacts' ('relay-smoke-' + [Guid]::NewGuid().ToString('N'))
    $null = New-Item -ItemType Directory -Path $BundlePath
    foreach ($project in @('Agent', 'Controller')) {
        $output = if ($project -eq 'Agent') { 'agent' } else { 'controller' }
        & $dotnet publish (Join-Path $repo 'src' "Microsoft.Crank.$project" "Microsoft.Crank.$project.csproj") `
            --framework net10.0 --configuration Release --output (Join-Path $BundlePath $output) --verbosity quiet
        if ($LASTEXITCODE -ne 0) { throw "Publishing $project failed." }
    }
    Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'Start-Agent.ps1') -Destination $BundlePath
    Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'Start-Agent.sh') -Destination $BundlePath
    Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'Run.ps1') -Destination $BundlePath
    $null = New-Item -ItemType Directory -Path (Join-Path $BundlePath 'workload')
    foreach ($file in @('RelaySmoke.csproj', 'Program.cs')) {
        Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'workload' $file) -Destination (Join-Path $BundlePath 'workload')
    }
}
$BundlePath = (Resolve-Path -LiteralPath $BundlePath).Path
foreach ($file in @((Join-Path 'agent' 'crank-agent.dll'), (Join-Path 'controller' 'crank.dll'),
    (Join-Path 'workload' 'RelaySmoke.csproj'), (Join-Path 'workload' 'Program.cs'))) {
    if (!(Test-Path -LiteralPath (Join-Path $BundlePath $file))) { throw "Incomplete bundle: $file" }
}
Write-Host "Smoke bundle: $BundlePath"
if ($PrepareOnly) { return }

$run = Join-Path $BundlePath ('run-' + [Guid]::NewGuid().ToString('N'))
foreach ($name in @('', 'source', 'payloads', 'downloads', 'temp', 'agent-build', 'agent-dotnet')) {
    $null = New-Item -ItemType Directory -Path (Join-Path $run $name)
}
$agentCommand = $null
$controllerCommand = $null
$oldTemp, $oldTmp, $oldTmpDir = $env:TEMP, $env:TMP, $env:TMPDIR
$started = [DateTimeOffset]::UtcNow
$summary = [ordered]@{ status = 'failed'; started = $started; bundle = $BundlePath; run = $run; relay = [bool]$Relay }
try {
    $env:TEMP = $env:TMP = $env:TMPDIR = Join-Path $run 'temp'
    if (!$Endpoint) {
        $reservation = [Net.Sockets.TcpListener]::new([Net.IPAddress]::Loopback, 0)
        $reservation.Start()
        $port = $reservation.LocalEndpoint.Port
        $reservation.Stop()
        $Endpoint = [uri]"http://127.0.0.1:$port"
        $agentCommand = Start-LoggedProcess $dotnet @(
            (Join-Path $BundlePath 'agent' 'crank-agent.dll'), '--url', $Endpoint.AbsoluteUri,
            '--no-cleanup', '--build-path', (Join-Path $run 'agent-build'),
            '--dotnethome', (Join-Path $run 'agent-dotnet')) 'agent' $run
        $client = [Net.Http.HttpClient]::new()
        $client.Timeout = [TimeSpan]::FromSeconds(2)
        try {
            $deadline = [DateTime]::UtcNow.AddSeconds(60)
            $ready = $false
            while ([DateTime]::UtcNow -lt $deadline) {
                if ($agentCommand.Process.HasExited) { throw 'Local agent exited before readiness. See agent logs.' }
                try {
                    $response = $client.GetAsync("$($Endpoint.AbsoluteUri.TrimEnd('/'))/jobs/info").GetAwaiter().GetResult()
                    try { $ready = $response.IsSuccessStatusCode } finally { $response.Dispose() }
                    if ($ready) { break }
                }
                catch [Net.Http.HttpRequestException] {
                    Write-Verbose "Waiting for agent: $($_.Exception.Message)"
                }
                catch [OperationCanceledException] {
                    Write-Verbose 'Agent readiness request timed out; retrying within startup deadline.'
                }
                Start-Sleep -Milliseconds 200
            }
            if (!$ready) { throw 'Local agent readiness timed out. See agent logs.' }
        }
        finally { $client.Dispose() }
    }
    if (!$ExpectedAgentUrl) { $ExpectedAgentUrl = $Endpoint.AbsoluteUri.TrimEnd('/') }
    $summary.endpoint = $Endpoint.AbsoluteUri
    $summary.expectedAgentUrl = $ExpectedAgentUrl
    Copy-Item -LiteralPath (Join-Path $BundlePath 'workload' 'RelaySmoke.csproj') -Destination (Join-Path $run 'source')
    Copy-Item -LiteralPath (Join-Path $BundlePath 'workload' 'Program.cs') -Destination (Join-Path $run 'source')
    # The uploaded project must not inherit build configuration from the agent's checkout.
    foreach ($file in @('Directory.Build.props', 'Directory.Build.targets', 'Directory.Packages.props')) {
        '<Project />' | Set-Content -LiteralPath (Join-Path $run 'source' $file)
    }
    @{ sdk = @{ version = $SdkVersion; rollForward = 'disable' } } |
        ConvertTo-Json -Depth 3 | Set-Content -LiteralPath (Join-Path $run 'source' 'global.json')
    New-Payload (Join-Path $run 'source' 'source-proof.bin') 65537
    New-Payload (Join-Path $run 'payloads' 'build-proof.bin') 8388609
    $outputFiles = @()
    foreach ($size in @(65535, 65536, 65537, 8388608)) {
        $path = Join-Path $run 'payloads' "payload-$size.bin"
        New-Payload $path $size
        $outputFiles += $path
    }
    $config = @{
        scenarios = @{ smoke = @{ application = @{ job = 'application' } } }
        profiles = @{ smoke = @{ jobs = @{ application = @{ selfContained = $false } } } }
        jobs = @{
            application = @{
                sources = @{ smoke = @{ localFolder = (Join-Path $run 'source'); destinationFolder = '' } }
                project = 'RelaySmoke.csproj'; framework = 'net10.0'; sdkVersion = $SdkVersion
                runtimeVersion = $RuntimeVersion; aspNetCoreVersion = $RuntimeVersion
                selfContained = $false; isConsoleApp = $true; waitForExit = $true; timeout = 60
                readyStateText = 'Relay smoke ready'; endpoints = @($Endpoint.AbsoluteUri.TrimEnd('/'))
                options = @{
                    collectCounters = $false; displayOutput = $true; displayBuild = $true
                    buildFiles = @((Join-Path $run 'payloads' 'build-proof.bin'))
                    outputFiles = $outputFiles
                    downloadFiles = @('smoke-result.json', 'returned-*.bin')
                    downloadFilesOutput = (Join-Path $run 'downloads')
                }
            }
        }
    }
    $configPath = Join-Path $run 'smoke.json'
    $config | ConvertTo-Json -Depth 20 | Set-Content -LiteralPath $configPath
    $arguments = @((Join-Path $BundlePath 'controller' 'crank.dll'), '--config', $configPath, '--scenario', 'smoke', '--profile', 'smoke',
        '--json', (Join-Path $run 'controller-result.json'))
    if ($Relay) {
        $arguments += '--relay'
        if ($RelayEnvironmentVariable) { $arguments += $RelayEnvironmentVariable }
    }
    Write-Host "Testing $Endpoint; logs and results: $run"
    $controllerCommand = Start-LoggedProcess $dotnet $arguments 'controller' $run
    if (!$controllerCommand.Process.WaitForExit($TimeoutSeconds * 1000)) {
        throw "Controller exceeded ${TimeoutSeconds}s; only the owned controller process will be stopped."
    }
    $exitCode = $controllerCommand.Process.ExitCode
    $null = $controllerCommand.OutTask.GetAwaiter().GetResult()
    $null = $controllerCommand.ErrTask.GetAwaiter().GetResult()
    if ($exitCode -ne 0) { throw "Controller failed with exit code $exitCode. See controller and agent logs." }

    $result = Get-Content -LiteralPath (Join-Path $run 'downloads' 'smoke-result.json') -Raw | ConvertFrom-Json -AsHashtable
    $expectedJobUrl = $ExpectedAgentUrl.TrimEnd('/') + '/jobs/' + $result.jobId
    if ($result.agentUrl.TrimEnd('/') -ne $ExpectedAgentUrl.TrimEnd('/') -or $result.jobLocalUrl -ne $expectedJobUrl) {
        throw "Exported benchmark URLs do not match ExpectedAgentUrl. See smoke-result.json."
    }
    if ($result.jobUrl -ne ($Endpoint.AbsoluteUri.TrimEnd('/') + '/jobs/' + $result.jobId)) {
        throw 'The public job URL did not retain the endpoint/entity prefix.'
    }
    $inputs = @((Join-Path $run 'source' 'source-proof.bin'), (Join-Path $run 'payloads' 'build-proof.bin')) + $outputFiles
    foreach ($inputFile in $inputs) {
        $name = [IO.Path]::GetFileName($inputFile)
        $expected = (Get-FileHash -LiteralPath $inputFile -Algorithm SHA256).Hash
        $downloaded = Join-Path $run 'downloads' "returned-$name"
        if ($result.files[$name].sha256 -ne $expected -or
            (Get-FileHash -LiteralPath $downloaded -Algorithm SHA256).Hash -ne $expected -or
            $result.files[$name].length -ne (Get-Item -LiteralPath $inputFile).Length) {
            throw "Uploaded/downloaded content verification failed for $name."
        }
    }
    $summary.status = 'passed'
    $summary.jobId = $result.jobId
    $summary.verifiedFiles = $inputs.Count
    Write-Host "PASS: job execution, raw source/output ZIPs, gzip build upload, six downloaded hashes, and exported URLs."
}
catch {
    $summary.error = $_.Exception.Message
    throw
}
finally {
    try { Close-LoggedProcess $controllerCommand }
    finally {
        try { Close-LoggedProcess $agentCommand }
        finally {
            $summary.finished = [DateTimeOffset]::UtcNow
            $summary | ConvertTo-Json -Depth 10 | Set-Content -LiteralPath (Join-Path $run 'summary.json')
            $env:TEMP, $env:TMP, $env:TMPDIR = $oldTemp, $oldTmp, $oldTmpDir
            Write-Host "Preserved diagnostics: $run"
        }
    }
}
