# Azure DevOps worker

`crank-azdo` consumes Crank jobs from Service Bus and reports their status and
output to Azure DevOps. Result conversion belongs in the controller's existing
[`afterJob` commands](../../docs/precommands.md), not a separate worker hook.

Install any command executable and its credentials on the controller/worker
host. Commands inherit its environment and the per-attempt working directory.
Only credential environment-variable names, not secrets, should appear in
benchmark configuration. A command failure fails the Crank attempt; ordinary
failures use the payload's retry count. Cancellation stops retries and
terminates the controller process tree, including command subprocesses.

The payload `timeout` covers the entire attempt, including `afterJob`.
Allow time for both the benchmark and export. The worker renews the message
lock for up to one day by default, configurable with
`--max-lock-renewal-duration <timespan>` or
`CRANK_AZDO_MAX_LOCK_RENEWAL_DURATION`. It rejects jobs whose timeout across all
attempts, plus a five-minute settlement margin, exceeds that renewal limit.
Attempt files are cleaned up after Crank and its commands finish.
