# Dynamic Azure Provisioning

This guide explains how to use crank's dynamic infrastructure provisioning feature to automatically provision Azure VMs for benchmarking, run tests, and tear down infrastructure — all in a single command.

## Overview

Instead of maintaining always-on machines with `crank-agent` installed, you can define a `provision` block in your profile that describes the infrastructure you need. Crank will:

1. **Provision** Azure VMs with `crank-agent` pre-installed
2. **Wait** for agents to become healthy
3. **Run** your benchmark scenario
4. **Tear down** all infrastructure automatically

## Prerequisites

- [Azure CLI](https://docs.microsoft.com/cli/azure/install-azure-cli) installed and authenticated (`az login`)
- An Azure subscription with permission to create VMs
- `crank` controller tool installed

## Quick Start

### 1. Define a provisioning profile

Add an `azure` profile to your benchmark configuration:

```yaml
profiles:
  azure:
    variables:
      serverAddress: "{{ serverAddress }}"
    agents:
      application:
        provision:
          provider: azure
          vmSize: Standard_D4s_v5
          os: linux
          region: eastus2
          image: ubuntu-22.04
      load:
        provision:
          provider: azure
          vmSize: Standard_D4s_v5
          os: linux
          region: eastus2
```

### 2. Run with dynamic provisioning

```bash
crank --config my.benchmarks.yml --scenario hello --profile azure
```

That's it! Crank will provision VMs, deploy agents, run the benchmark, and clean up.

## Profile Configuration Reference

The `provision` block supports the following properties:

| Property | Type | Default | Description |
|----------|------|---------|-------------|
| `provider` | string | `azure` | Cloud provider (currently only `azure`) |
| `vmSize` | string | `Standard_D4s_v5` | Azure VM SKU |
| `os` | string | `linux` | Operating system (`linux` or `windows`) |
| `region` | string | `eastus2` | Azure region |
| `image` | string | `ubuntu-22.04` | OS image (`ubuntu-22.04`, `ubuntu-24.04`, `windows-2022`, `windows-2025`) |
| `customImageId` | string | | Custom VM image resource ID (overrides `image`) |
| `count` | integer | `1` | Number of VM instances |
| `agentPort` | integer | `5010` | Port for the crank agent |
| `agentImage` | string | | Docker image containing crank-agent (uses Docker deployment) |
| `agentSource` | string | | Git repository URL to clone and build the agent from source |
| `agentSourceBranch` | string | `main` | Branch, tag, or commit SHA to checkout when building from source |
| `agentSourceProject` | string | `src/Microsoft.Crank.Agent/...csproj` | Relative path to agent project within the source repo |
| `subscriptionId` | string | | Azure subscription ID (uses default if not set) |
| `resourceGroup` | string | | Custom resource group name (auto-generated if not set) |
| `spotInstance` | boolean | `false` | Use Azure Spot VMs for cost savings |
| `tags` | object | | Additional tags for Azure resources |

## CLI Options

| Option | Description |
|--------|-------------|
| `--provision-timeout <minutes>` | Maximum wait time for agents to become ready (default: 10) |
| `--no-teardown` | Skip tearing down infrastructure after the run (for debugging) |
| `--provision-cleanup <hours>` | Clean up orphaned resource groups older than N hours |
| `--provision-pool <name>` | Name a pool to reuse VMs across consecutive runs |
| `--provision-pool-ttl <minutes>` | Time to keep a pool alive after the run (default: 30) |

## Examples

### Basic provisioning

```bash
crank --config hello.benchmarks.yml --scenario hello --profile azure
```

### With Spot VMs for cost savings

```yaml
profiles:
  azure-spot:
    agents:
      application:
        provision:
          provider: azure
          vmSize: Standard_D8s_v5
          os: linux
          spotInstance: true
      load:
        provision:
          provider: azure
          vmSize: Standard_D4s_v5
          os: linux
          spotInstance: true
```

### Using a Docker-based agent

```yaml
profiles:
  azure-docker:
    agents:
      application:
        provision:
          provider: azure
          vmSize: Standard_D4s_v5
          agentImage: mcr.microsoft.com/dotnet/crank-agent:latest
```

### Building the agent from source

Useful for testing agent changes or running a specific commit/branch:

```yaml
profiles:
  azure-source:
    agents:
      application:
        provision:
          provider: azure
          vmSize: Standard_D4s_v5
          agentSource: https://github.com/dotnet/crank.git
          agentSourceBranch: my-feature-branch
```

You can also specify a commit SHA:

```yaml
          agentSourceBranch: abc123def456
```

Or point to a fork with a custom project path:

```yaml
          agentSource: https://github.com/my-org/custom-crank.git
          agentSourceBranch: main
          agentSourceProject: src/MyCustomAgent/MyCustomAgent.csproj
```

### With a custom VM image (fastest startup)

```yaml
profiles:
  azure-custom:
    agents:
      application:
        provision:
          provider: azure
          vmSize: Standard_D4s_v5
          customImageId: /subscriptions/.../images/crank-agent-ubuntu
```

### Debugging — keep VMs alive after run

```bash
crank --config hello.benchmarks.yml --scenario hello --profile azure --no-teardown
```

### Clean up orphaned resources

If a run crashes and leaves VMs behind:

```bash
crank --provision-cleanup 2
```

This deletes all crank-managed resource groups older than 2 hours.

## Reusing VMs with Provision Pools

When running multiple benchmarks in succession with the same profile, you can avoid reprovisioning VMs each time by using **provision pools**. A named pool keeps VMs alive between runs and reuses them if healthy.

### First run — provisions and names the pool

```bash
crank --config hello.benchmarks.yml --scenario hello --profile azure --provision-pool my-bench
```

### Subsequent runs — reuses existing VMs

```bash
crank --config hello.benchmarks.yml --scenario scenario2 --profile azure --provision-pool my-bench
```

If the pool's VMs are still running and healthy, they're reused instantly (no provisioning delay). The pool's TTL is extended after each run.

### Controlling pool lifetime

By default, pools are kept alive for 30 minutes after the last run. Customize with:

```bash
crank --config hello.benchmarks.yml --scenario hello --profile azure \
  --provision-pool my-bench --provision-pool-ttl 60
```

### How it works

1. On each run, the controller checks Azure for a resource group named `rg-crank-pool-{name}`
2. If found and agents are healthy → reuse them, extend the TTL
3. If not found or agents are unhealthy → provision fresh VMs with the pool name
4. After the run, VMs are kept alive (not torn down) with an auto-delete tag
5. Expired pools are automatically cleaned up at the start of the next provisioned run

### Automatic cleanup

Every time crank runs with dynamic provisioning, it automatically scans for and deletes any crank-managed resource groups that have passed their `auto-delete-after` time. This means expired pools are cleaned up as a side effect of normal usage — no manual intervention needed as long as crank is run periodically.

> **Important:** If crank is not run again after a pool expires, the VMs will continue running in Azure and incurring costs. If you stop using crank or won't run it again for a while, clean up leftover pools manually:
>
> ```bash
> # Delete all crank-managed resources older than 1 hour
> crank --provision-cleanup 1
> ```
>
> Or delete the resource groups directly in the [Azure Portal](https://portal.azure.com) — they are named `rg-crank-pool-{name}` and tagged with `crank-managed-by: crank-controller`.

### Cleaning up pools manually

```bash
# Clean up all pools/resources older than 1 hour
crank --provision-cleanup 1
```

## Mixing static and dynamic profiles

Existing static profiles continue to work unchanged. You can even combine them:

```yaml
profiles:
  # Static endpoint (existing infrastructure)
  lab:
    agents:
      application:
        endpoints:
          - http://perf-server:5010
      load:
        endpoints:
          - http://perf-client:5010

  # Dynamic Azure provisioning
  azure:
    agents:
      application:
        provision:
          provider: azure
          vmSize: Standard_D4s_v5
      load:
        provision:
          provider: azure
          vmSize: Standard_D4s_v5
```

Use `--profile lab` for static infrastructure or `--profile azure` for dynamic.

## Authentication

Dynamic provisioning uses Azure authentication in this order:

1. **Azure CLI** (`az login`) — recommended for local development
2. **Managed Identity** — for running in Azure VMs or containers
3. **Environment variables** — `AZURE_CLIENT_ID`, `AZURE_TENANT_ID`, `AZURE_CLIENT_SECRET`

## Cost Considerations

| VM SKU | $/hour | 10-min benchmark cost |
|--------|--------|-----------------------|
| Standard_D2s_v5 (2 vCPU) | ~$0.096 | ~$0.016 |
| Standard_D4s_v5 (4 vCPU) | ~$0.192 | ~$0.032 |
| Standard_D8s_v5 (8 vCPU) | ~$0.384 | ~$0.064 |
| Standard_D16s_v5 (16 vCPU) | ~$0.768 | ~$0.128 |

Using Spot VMs can reduce costs by up to 90%.

## Architecture

```
┌──────────────┐     1. Provision VMs       ┌──────────────────┐
│   Controller │ ─────────────────────────►  │   Azure ARM API  │
│   (crank CLI)│                             └──────────────────┘
│              │     2. Wait for healthy              │
│              │ ◄─── GET /jobs/info ────────  ┌──────┴──────┐
│              │                              │ VM + Agent  │
│              │     3. Submit jobs            │ (crank-agent)│
│              │ ─── POST /jobs ────────────► │ :5010        │
│              │                              └──────────────┘
│              │     4. Collect results
│              │ ◄─── GET /jobs/{id} ─────
│              │
│              │     5. Teardown
│              │ ─── DELETE Resource Group ─►  Azure ARM API
└──────────────┘
```

## Troubleshooting

### Agents not becoming ready

- Increase timeout: `--provision-timeout 15`
- Check cloud-init logs on the VM: `/var/log/crank-agent-setup.log`
- Use `--no-teardown` to keep VMs alive for debugging

### Azure quota limits

If you hit VM quota limits, request an increase in the Azure portal or try a different region.

### Orphaned resources

Expired pools and crashed-run leftovers are automatically cleaned up at the start of each provisioned run. However, if you stop using crank entirely, leftover resource groups will remain in Azure until manually deleted.

Use `--provision-cleanup 2` to find and delete resource groups left behind by failed runs, or delete `rg-crank-*` resource groups in the Azure portal. All crank-managed resource groups are tagged with `crank-managed-by: crank-controller`.
