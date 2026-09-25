# Azure DevOps worker

`crank-azdo` consumes Crank jobs from Service Bus and reports their status and
output to Azure DevOps. Result conversion belongs in the controller's existing
[`afterJob` commands](../../docs/precommands.md), not a separate worker hook.

Install any command executable and its credentials on the controller/worker
host. Commands inherit its environment and the per-attempt working directory.
Only credential environment-variable names, not secrets, should appear in
benchmark configuration. A command failure fails the Crank attempt; ordinary
failures use the payload's retry count. Cancellation stops retries and uses
the worker's existing shutdown sequence. Changes to subprocess shutdown
behavior are outside this PR.

The payload `timeout` covers the entire attempt, including `afterJob`.
Allow time for both the benchmark and export. The worker's existing one-hour
maximum message lock renewal duration is unchanged.
Attempt files are cleaned up after Crank and its commands finish.
