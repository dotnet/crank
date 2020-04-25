# Microsoft.Benchmarks

This is the benchmarking infrastructure used by the .NET team using run including (but not limited to) scenarios from the [TechEmpower Web Framework Benchmarks](https://www.techempower.com/benchmarks/).

## Components

The benchmarking infrastructure is made of these components:
- [benchmarks-agent](src/Microsoft.Benchmarks.Agent) - An application that executes jobs that run as part of a benchmarked.
- [benchmarks](src/Microsoft.Benchmarks.Controller) - An application that can enqueue jobs and display the results locally (or store them in a database).

There are also some built in jobs:
- [wrk](src/Microsoft.Benchmarks.Jobs.Wrk) - An http client benchmarking tool. This tool is used when benchmarking TechEmpower https://github.com/wg/wrk.
- [wrk2](src/Microsoft.Benchmarks.Jobs.Wrk2) - An http client benchmarking tool optmized for latency testing https://github.com/giltene/wrk2.
- [bombardier](Microsoft.Benchmarks.Jobs.Bombardier) - A go based http client benchmarking tool https://github.com/codesenberg/bombardier.

## Get Started

See the [Documentation](docs)

## How to Engage, Contribute, and Give Feedback

TBD

## Code of conduct

See [CODE-OF-CONDUCT](./CODE-OF-CONDUCT.md)
