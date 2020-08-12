Running development version
===========================

If you want to try latest and greatest crank you can run agent from `src\Microsoft.Crank.Agent` folder by simply running

    cd src\Microsoft.Crank.Agent
    dotnet run

To run development version of crank

    cd Microsoft.Crank.Controller
    dotnet run  --config ../../samples/hello/hello.benchmarks.yml --scenario hello --profile local --application.options.displayOutput true
