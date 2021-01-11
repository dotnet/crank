# Microsoft.Crank

[![Build Status](https://dnceng.visualstudio.com/public/_apis/build/status/dotnet/crank/dotnet-crank-ci-public?branchName=master)](https://dnceng.visualstudio.com/public/_build/latest?definitionId=848&branchName=master)

Crank is the benchmarking infrastructure used by the .NET team to run benchmarks including (but not limited to) scenarios from the [TechEmpower Web Framework Benchmarks](https://www.techempower.com/benchmarks/).

One of the goals of this system is to give developers a tool that lets them work on performance and measure potential improvements very easily. Some of the capabilities are:

- Deploy and benchmark multi-tiered applications based on .NET or Docker containers
- Store results in JSON or in SQL Server for charting
- Customize the applications or their environment to test different settings 
- Collect traces

Want to learn more? Check out our [documentation](docs/README.md).

## Components

The benchmarking infrastructure is made of these components:
- [crank-agent](src/Microsoft.Crank.Agent) - A service that executes jobs that run as part of a benchmark
- [crank](src/Microsoft.Crank.Controller) - A command line utility that can enqueue jobs and record results

There are also some built in jobs:
- [wrk](src/Microsoft.Crank.Jobs.Wrk) - An http client benchmarking tool. This tool is used when benchmarking TechEmpower https://github.com/wg/wrk.
- [wrk2](src/Microsoft.Crank.Jobs.Wrk2) - An http client benchmarking tool optimized for latency testing https://github.com/giltene/wrk2.
- [bombardier](Microsoft.Crank.Jobs.Bombardier) - A go based http client benchmarking tool https://github.com/codesenberg/bombardier.

## Get Started

- Read the [documentation](docs)
- Install the crank controller dotnet tool: `dotnet tool update Microsoft.Crank.Controller --version "0.2.0-*" --global`
- Use some [predefined scenarios](https://github.com/aspnet/Benchmarks/tree/master/scenarios)

## How to Engage, Contribute, and Give Feedback

If you want a *low-spam* way to follow what the team is doing you should subscribe to the issue we use to post announcements [here](https://github.com/dotnet/crank/issues/27). Only team-members can post on this issue.

Some of the best ways to contribute are to try things out, file issues, join in design conversations, and make pull-requests.

- Download our latest daily builds
- Try tutorials and working with your own projects
- Log issues if you find problems, or if you have suggestions.
- Log an issue if you have feedback you want to share with the team.

Check out the [contributing](/CONTRIBUTING.md) page to see the best places to log issues and start discussions.

## Code of conduct

This project has adopted the code of conduct defined by the Contributor Covenant to clarify expected behavior in our community. For more information, see the .NET Foundation Code of Conduct.
