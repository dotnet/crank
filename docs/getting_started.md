
### Define the job

```yml
imports:
    - https://raw.githubusercontent.com/aspnet/Benchmarks/master/src/BombardierClient/bombardier.yml

jobs:
  server:
    source:
      repository: https://github.com/aspnet/perf
      branchOrCommit: master
      project: samples/hello/hello.csproj

scenarios:
  hello:
    application:
      job: server
    load:
      job: bombardier
      variables:
        serverPort: 5000
        path: /

profiles:
  local:
    variables:
      serverUri: http://localhost
    jobs: 
      application:
        endpoints: 
          - http://localhost:5010
      load:
        endpoints: 
          - http://localhost:5011
```

### Run the Agent

TBD

### Run a Job using the controller

TBD

#### Optional: Storing the results

The controller can store the results of a job by passing a `-q [connectionstring]` argument. The connection
string must point to an existing SQL Server database. The first time it's called the required table will be created.
From there you can create reports using the tools of your choice.
