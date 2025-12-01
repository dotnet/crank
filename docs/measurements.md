## Description

This document demonstrates the different way to report custom measurements from a job.

## Metadata and Measurements

Measurements are timestamped values. When a job is running measurements will be collected automatically by the agent, like
CPU and Working Set. Jobs can also report their own measurements, like _bombardier_ which will report Requests Per Second and 
Latency values.

A __Measurement__ is associated to a mandatory __Metadata__. The metadata describes the measurements.

NB: You can see the metadata as the counter, and the measurements as the different values for this counter.

Here is an example of a metadata object:

```json
{
    "source": "Host Process",
    "name": "benchmarks/cpu",
    "aggregate": "Max",
    "reduce": "Max",
    "format": "n0",
    "longDescription": "Amount of time the process has utilized the CPU out of 100%",
    "shortDescription": "Max CPU Usage (%)"
}
```

And some measurements associated to this metadata:

```json
[
  {
    "name": "benchmarks/cpu",
    "timestamp": "2024-02-23T13:01:56.12Z",
    "value": 98
  },
  {
    "name": "benchmarks/cpu",
    "timestamp": "2024-02-23T13:02:56.12Z",
    "value": 99
  }
]
```

### Metadata Properties:

- `name`: The identifier of the metadata.
- `aggregate`: The algorithm used to aggregate all the measurement values from a single job.
- `reduce`: The algorithm used to reduce all the jobs aggregates to a single one.
- `format`: The C# format string used when displaying the result.
- `longDescription`: Long description of the metadata.
- `shortDescription`: Description use in the crank CLI output tables.

### Measurement Properties:

- `name`: The identifier of the corresponding metadata.
- `timestamp`: The moment in time when the measurement was taken.
- `value`: The value of the measurement.

## Reporting Metadata and Measurements

### From .NET applications

A NuGet package [Microsoft.Crank.EventSources](https://www.nuget.org/packages/Microsoft.Crank.EventSources) contains an API that can record metadata and 
measurements using an Event Source. This is the recommended approach as it provides a fast and strongly type method.

After referencing the package from your project, here is an example to record metadata and measurements:

Register metadata once:

```c#
BenchmarksEventSource.Register("http/requests", Operations.Max, Operations.Sum, "Requests", "Total number of requests", "n0");
BenchmarksEventSource.Register("http/requests/badresponses", Operations.Max, Operations.Sum, "Bad responses", "Non-2xx or 3xx responses", "n0");
```

And then record as many measurements as you want. The timestamp is recorded automatically.

```c#
BenchmarksEventSource.Measure("http/requests", total);
BenchmarksEventSource.Measure("http/requests/badresponses", total - success);
```

### Using the Web API

Each agent can record custom metadata nad measurements using its own endpoints.

#### Metadata endpoint:

```
{agentUrl}/metadata?name=cpu&aggregate=max&reduce=max&format=n0&longDescription=Long%20description&shortDescription=Short%20description
```

#### Measurement endpoint:

```
{agentUrl}/measurement?name=cpu&timestamp=2024-02-23T14:00:00Z&value=123.456
```

It is expected the agent url to be `http://localhost:5001` since the job is running on the same instance as the agent. The port might vary based on the deployments.

### Console 

Simply writing on the console lets you record these values. The message must start with `#StartJobStatistics` and end with `#EndJobStatistics`.
Between these delimiters inject the following json payload.

```json
{
  "Metadata": [],
  "Measurements": []
}
```

NB: This can be repeated several times and you can omit the metadata or the measurements if necessary.

Here is a working example:

```json
{
  "Metadata": [
    {
      "source": "Host Process",
      "name": "benchmarks/cpu",
      "aggregate": "Max",
      "reduce": "Max",
      "format": "n0",
      "longDescription": "Amount of time the process has utilized the CPU out of 100%",
      "shortDescription": "Max CPU Usage (%)"
    }],
  "Measurements": [
    {
      "name": "benchmarks/cpu",
      "timestamp": "2024-02-23T13:01:56.12Z",
      "value": 98
    },
    {
      "name": "benchmarks/cpu",
      "timestamp": "2024-02-23T13:02:56.12Z",
      "value": 99
    }]
}
```

### From the Agent Command Line

The agent can be configured to record custom measurements for every benchmark it runs using the `--record` (or `-r`) command line option. This is useful for recording system information or configuration details that should be captured for all benchmarks.

#### Usage

```bash
crank-agent --record "name=value"
```

The option can be specified multiple times to record multiple measurements:

```bash
crank-agent --record "system/openssl=$(openssl version)" --record "system/kernel=$(uname -r)"
```

#### Format

- The name and value are separated by the first `=` character
- The name portion becomes the measurement name
- The value can be a literal string

#### Command Substitution

Command substitution is handled by the shell before the arguments reach the agent. Use your shell's command substitution syntax:

**Linux/macOS example (using bash/sh):**
```bash
crank-agent --record "system/openssl=$(openssl version)" \
            --record "system/kernel=$(uname -r)" \
            --record "system/hostname=$(hostname)"
```

**Windows example (using PowerShell):**
```powershell
crank-agent --record "system/dotnet=$(dotnet --version)" `
            --record "system/os=$(systeminfo | Select-String 'OS Name')"
```

#### Behavior

- Custom measurements are automatically added to every job that the agent runs
- Each measurement includes metadata with:
  - `Source`: "Agent"
  - `Aggregate`: First
  - `Reduce`: First
  - `ShortDescription`: The measurement name
  - `LongDescription`: "Custom measurement: {name}"
- Invalid formats (missing `=` or empty name) will be logged as warnings and skipped

#### Example

Start an agent that records the OpenSSL version:

```bash
crank-agent --record "system/openssl=$(openssl version)"
```

When this agent runs a benchmark, the measurement will automatically include:

```json
{
  "name": "system/openssl",
  "timestamp": "2024-02-23T13:01:56.12Z",
  "value": "OpenSSL 3.0.13 30 Jan 2024"
}
```
