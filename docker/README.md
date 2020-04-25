# Benchmarks in Docker

## BenchmarksDriver (Coordinates Client and Server)
```
git clone https://github.com/aspnet/benchmarks
cd benchmarks/docker/benchmarks
./build.sh
./run-driver.sh -s http://server:5001 -c http://client:5002 -n scenario
```

## BenchmarksClient (Load Generator)
```
git clone https://github.com/aspnet/benchmarks
cd benchmarks/docker/benchmarks
./build.sh
./run-client.sh
```

## BenchmarksServer (Web Server)
```
git clone https://github.com/aspnet/benchmarks
cd benchmarks/docker/benchmarks
./build.sh
./run-server.sh
```