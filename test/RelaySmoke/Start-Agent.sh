#!/usr/bin/env bash
# Licensed to the .NET Foundation under one or more agreements.
# The .NET Foundation licenses this file to you under the MIT license.
# See the LICENSE file in the project root for more information.

set -euo pipefail

usage() {
    cat <<'EOF'
Usage: bash Start-Agent.sh [options]

  --bundle-path PATH              Published bundle containing agent/crank-agent.dll.
                                  Default: directory containing this script.
  --work-path PATH                New dedicated workspace; its parent must exist.
                                  Default: a unique agent-run.* directory in the bundle.
  --url URL                      HTTP binding (default: http://127.0.0.1:5010).
  --relay-env NAME                Environment variable containing the listen connection
                                  string, including EntityPath. Never pass the secret itself.
  --enable-http                  Enable HTTP alongside Relay.
  --managed-identity-client-id ID Forward --mi-client-id for Relay authentication.
  -h, --help                     Show this help.

Runs in the foreground with --no-cleanup and isolated build/SDK/temp/log directories.
Stop with Ctrl+C or SIGTERM. Nothing is installed and no cloud resources are created.
Use a separate canary entity, never an entity already hosting another agent.
EOF
}

fail() {
    printf 'Error: %s\n' "$*" >&2
    exit 1
}

require_value() {
    [[ $# -ge 2 && -n $2 && $2 != --* ]] || fail "$1 requires a value."
}

bundle_path=$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")" && pwd -P)
work_path=''
url='http://127.0.0.1:5010'
relay_env=''
enable_http=false
managed_identity_client_id=''

while (($#)); do
    case "$1" in
        --bundle-path) require_value "$@"; bundle_path=$2; shift 2 ;;
        --work-path) require_value "$@"; work_path=$2; shift 2 ;;
        --url) require_value "$@"; url=$2; shift 2 ;;
        --relay-env) require_value "$@"; relay_env=$2; shift 2 ;;
        --enable-http) enable_http=true; shift ;;
        --managed-identity-client-id) require_value "$@"; managed_identity_client_id=$2; shift 2 ;;
        -h|--help) usage; exit 0 ;;
        *) fail 'Unknown option. Use --help for supported options.' ;;
    esac
done

[[ -d $bundle_path ]] || fail "Bundle directory not found: $bundle_path"
bundle_path=$(cd -- "$bundle_path" && pwd -P)
agent="$bundle_path/agent/crank-agent.dll"
[[ -f $agent ]] || fail "Published agent not found: $agent"
command -v dotnet >/dev/null 2>&1 || fail 'dotnet is not on PATH. Install the .NET 10 ASP.NET Core runtime first.'

if [[ -n $relay_env ]]; then
    [[ $relay_env =~ ^[a-zA-Z_][a-zA-Z0-9_]*$ ]] || fail '--relay-env must be an environment variable name, not a connection string.'
    case "$relay_env" in
        TEMP|TMP|TMPDIR) fail 'The launcher reserves TEMP, TMP and TMPDIR for its isolated workspace.' ;;
    esac
    [[ -n ${!relay_env:-} ]] || fail "Set the listen connection string in environment variable $relay_env first."
elif [[ $enable_http == true || -n $managed_identity_client_id ]]; then
    fail '--enable-http and --managed-identity-client-id require --relay-env.'
fi

umask 077
if [[ -n $work_path ]]; then
    # No -p: a pre-existing directory (or symlink) must never be reused.
    mkdir -- "$work_path" || fail 'Cannot create workspace. It must be new and its parent must exist.'
else
    work_path=$(mktemp -d "$bundle_path/agent-run.XXXXXXXX")
fi
work_path=$(cd -- "$work_path" && pwd -P)
mkdir -- "$work_path/build" "$work_path/dotnet" "$work_path/temp" "$work_path/logs"

arguments=("$agent" --no-cleanup
    --build-path "$work_path/build" --dotnethome "$work_path/dotnet"
    --log-path "$work_path/logs" --url "$url")
if [[ -n $relay_env ]]; then
    export "$relay_env"
    arguments+=(--relay "$relay_env")
    if [[ $enable_http == true ]]; then
        arguments+=(--relay-enable-http)
    fi
fi
if [[ -n $managed_identity_client_id ]]; then
    arguments+=(--mi-client-id "$managed_identity_client_id")
fi

export TEMP="$work_path/temp" TMP="$work_path/temp" TMPDIR="$work_path/temp"
printf 'Dedicated agent workspace: %s\n' "$work_path"
printf 'Use only a separate canary entity. Stop this foreground agent with Ctrl+C or SIGTERM.\n'
# Replace the shell so signals and the agent exit status reach the caller directly.
exec dotnet "${arguments[@]}"
