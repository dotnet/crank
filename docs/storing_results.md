## Storing benchmark results

This tutorial shows how to store the results of a benchmark locally and in a SQL Server database.

### Local JSON files

Use `--output [filename]` to store the results locally in JSON format. 

Some jobs can provide more information when the results are stored than on the console. For instance __bombardier__ will also provide detailed latency information that is not displayed by default.

The following example executes the __hello__ sample benchmark and stores the results in a file named `results.json`. We assume the agent is running locally.

```
crank --config /crank/samples/hello/hello.benchmarks.yml --scenario hello --profile local --output results.json 
```

And the file should contain some content similar to this:

```json
{
  "jobResults": {
    "jobs": {
      "application": {
        "results": {
          "benchmarks/cpu": 48.0,
          "benchmarks/cpu/raw": 192.59554622249087,
          "benchmarks/working-set": 74.0,
          "benchmarks/build-time": 17161.0,
          "benchmarks/published-size": 86797.0
        },
        "metadata": {},
        "measurements": {},
        "environment": {},
      },
      "load": {
        "results": {
          "benchmarks/cpu": 3.0,
          "benchmarks/cpu/raw": 13.810049532535345,
          "benchmarks/working-set": 37.0,
          "benchmarks/build-time": 6603.0,
          "benchmarks/published-size": 68199.0,
          "bombardier/requests": 459658.0,
          "bombardier/badresponses": 0.0,
          "bombardier/latency/mean": 8353.192049306223,
          "bombardier/latency/max": 564036.0,
          "bombardier/rps/mean": 30637.880336038248,
          "bombardier/rps/max": 46484.549118067116,
          "bombardier/raw": [
            {
              "spec": {
                "numberOfConnections": 256,
                "testType": "timed",
                "testDurationSeconds": 15,
                "method": "GET",
                "url": "http://localhost:5000/",
                "body": "",
                "stream": false,
                "timeoutSeconds": 2,
                "client": "fasthttp"
              },
              "result": {
                "bytesRead": 56537934,
                "bytesWritten": 28498796,
                "timeTakenSeconds": 15.0090729,
                "req1xx": 0,
                "req2xx": 459658,
                "req3xx": 0,
                "req4xx": 0,
                "req5xx": 0,
                "others": 0,
                "latency": {
                  "mean": 8353.192049306223,
                  "stddev": 4385.638938510043,
                  "max": 564036,
                  "percentiles": {
                    "50": 7995,
                    "75": 8994,
                    "90": 10992,
                    "95": 11992,
                    "99": 15989
                  }
                },
                "rps": {
                  "mean": 30637.880336038248,
                  "stddev": 5525.200372272327,
                  "max": 46484.549118067116,
                  "percentiles": {
                    "50": 31656.22072,
                    "75": 34329.346203,
                    "90": 37023.77316,
                    "95": 38497.187757,
                    "99": 40848.999965
                  }
                }
              }
            }
          ]
        }
      }
    }
  }
}
```

Each service that is deployed gets an entry in the `jobResults.jobs` property. In this example `application` and `load`. Then each service contains the following properties:

- `results`: a set of properties that are computed and displayed by **crank**
- `metadata`: and array describing each property of `results`
- `measurements`: every single value measured by the service
- `environment`: a set of properties describing the environment the service ran on

### SQL Server database

Use `--sql [connection-string] --table [table-name]` arguments to store in the specified SQL Server database. The connection string must point to an existing SQL Server database. The first time it's called the required table will be created.

The created table has the following schema:


| Column         | Type     | Example     | Description     |
| :------------- | :---------- | :----------- | :----------- |
| Id | `int` | 1 | auto-incremented identifer |
| Excluded | `bit` | false | flag used to ignore a result for soft deletion |
| DateTimeUtc | `datetimeoffset` | 2020-06-01T00:00:00Z | date and time when the job was saved |
| Session | `nvarchar(200)` | 20200601.1 | custom string representing a job logical identifier |
| Scenario | `nvarchard(200)` | hello | name of the scenario that was used |
| Description | `nvarchard(200)` | custom string representing extra information about the job | |
| Document | `nvarchar(max)` | { jobResults: {} } | json document containing the results of the job |

### Elasticsearch

Use `--es [server-url] --index [index-name]` arguments to store in the specified Elasticsearch server. The url must point to an existing Elasticsearch instance. The first time it's called the mapping of the required index will be created.

The created index has the same set of fields as SqlServer