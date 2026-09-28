#!/usr/bin/env bash
# Licensed to the .NET Foundation under one or more agreements.
# The .NET Foundation licenses this file to you under the MIT license.
# See the LICENSE file in the project root for more information.

set -euo pipefail

script_dir=$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")" && pwd -P)
root=$(mktemp -d "${TMPDIR:-/tmp}/crank-relay-launcher-tests.XXXXXXXX")
root=$(cd -- "$root" && pwd -P)
printf 'Launcher test artifacts: %s\n' "$root"
mkdir -- "$root/bin" "$root/bundle with spaces" "$root/bundle with spaces/agent"
bundle="$root/bundle with spaces"
cp -- "$script_dir/Start-Agent.sh" "$bundle/Start-Agent.sh"
touch "$bundle/agent/crank-agent.dll"

cat > "$root/bin/dotnet" <<'EOF'
#!/usr/bin/env bash
set -euo pipefail
printf '%s\0' "$@" > "$CAPTURE.args"
printf '%s\n' "$TEMP" "$TMP" "$TMPDIR" > "$CAPTURE.temp"
printf '%s\n' "${CRANK_RELAY_LISTEN:+present}" > "$CAPTURE.auth"
exit "${DOTNET_EXIT_CODE:-0}"
EOF
chmod +x "$root/bin/dotnet"
export PATH="$root/bin:$PATH"
export CAPTURE="$root/capture"

fail() {
    printf 'FAIL: %s\n' "$*" >&2
    exit 1
}

run_launcher() {
    if bash "$bundle/Start-Agent.sh" "$@" > "$CAPTURE.stdout" 2> "$CAPTURE.stderr"; then
        actual_exit=0
    else
        actual_exit=$?
    fi
}

read_arguments() {
    arguments=()
    while IFS= read -r -d '' argument; do
        arguments+=("$argument")
    done < "$CAPTURE.args"
}

expect_failure() {
    run_launcher "$@"
    [[ $actual_exit -ne 0 ]] || fail 'Invalid invocation succeeded.'
    [[ -s "$CAPTURE.stderr" ]] || fail 'Invalid invocation did not report an error.'
}

run_launcher --help
[[ $actual_exit == 0 ]] || fail 'Help failed.'
[[ ! -e "$CAPTURE.args" ]] || fail 'Help launched dotnet.'

expect_failure --bundle-path
expect_failure --work-path --enable-http
expect_failure --unknown
expect_failure --bundle-path "$root/missing"
expect_failure --enable-http
expect_failure --managed-identity-client-id test
expect_failure --relay-env 'not-a-variable'
expect_failure --relay-env 'Endpoint=sb://do-not-log.invalid'
! grep -q 'do-not-log' "$CAPTURE.stderr" || fail 'Connection string leaked in validation output.'
unset CRANK_RELAY_LISTEN
expect_failure --relay-env CRANK_RELAY_LISTEN
expect_failure --relay-env TMPDIR
[[ ! -e "$CAPTURE.args" ]] || fail 'Invalid invocation launched dotnet.'

work="$root/http work"
run_launcher --work-path "$work" --url 'http://127.0.0.1:5077'
[[ $actual_exit == 0 ]] || fail 'HTTP launch failed.'
read_arguments
expected=("$bundle/agent/crank-agent.dll" --no-cleanup --build-path "$work/build"
    --dotnethome "$work/dotnet" --log-path "$work/logs" --url 'http://127.0.0.1:5077')
[[ ${#arguments[@]} == ${#expected[@]} ]] || fail 'Unexpected HTTP argument count.'
for index in "${!expected[@]}"; do
    [[ ${arguments[index]} == "${expected[index]}" ]] || fail "HTTP argument $index was not preserved."
done
[[ $(cat "$CAPTURE.temp") == "$(printf '%s\n%s\n%s' "$work/temp" "$work/temp" "$work/temp")" ]] ||
    fail 'Temp directories were not isolated.'
for directory in build dotnet temp logs; do
    [[ -d "$work/$directory" ]] || fail "Missing $directory directory."
done
[[ -z $(cat "$CAPTURE.auth") ]] || fail 'HTTP mode injected authentication.'
touch "$work/keep"
expect_failure --work-path "$work"
[[ -f "$work/keep" ]] || fail 'Existing workspace was modified.'
ln -s "$work" "$root/work-link"
expect_failure --work-path "$root/work-link"

export CRANK_RELAY_LISTEN='fake-secret-for-launcher-test'
run_launcher --relay-env CRANK_RELAY_LISTEN
[[ $actual_exit == 0 ]] || fail 'Relay launch failed.'
read_arguments
[[ ${#arguments[@]} == 12 && ${arguments[10]} == --relay && ${arguments[11]} == CRANK_RELAY_LISTEN ]] ||
    fail 'Relay was not passed by environment variable name.'
[[ ${arguments[3]} == "$bundle"/agent-run.*/build ]] || fail 'Default workspace is not unique within bundle.'
[[ $(cat "$CAPTURE.auth") == present ]] || fail 'Listen environment was not inherited.'
! grep -q 'fake-secret-for-launcher-test' "$CAPTURE.args" "$CAPTURE.stdout" "$CAPTURE.stderr" ||
    fail 'Listen secret leaked into arguments or launcher output.'
first_work=${arguments[3]}
run_launcher --relay-env CRANK_RELAY_LISTEN --enable-http --managed-identity-client-id identity-id
[[ $actual_exit == 0 ]] || fail 'Combined mode launch failed.'
read_arguments
[[ ${#arguments[@]} == 15 && ${arguments[12]} == --relay-enable-http &&
    ${arguments[13]} == --mi-client-id && ${arguments[14]} == identity-id ]] ||
    fail 'Combined mode or identity arguments were not forwarded.'
[[ ${arguments[3]} != "$first_work" ]] || fail 'Default workspace was reused.'

export DOTNET_EXIT_CODE=23
run_launcher --bundle-path "$bundle" --work-path "$root/failed-agent"
[[ $actual_exit == 23 ]] || fail 'Agent exit status was not propagated.'
printf 'PASS: HTTP, Relay-only, combined mode, identity, quoting, validation, isolation, secret handling and exit status.\n'
