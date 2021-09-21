// Sample script file for HttpClient Crank client

// This script has the same lifetime as the HttpClient thread.

// Available properties:
// - console
//      log(args)
//      info(args)
//      warn(args)
//      error(args)
//      hasErrors: bool

function initialize(url, connections, warmup, duration, headers, version, quiet) {

    // Invoked before the client is created. Once per thread.
    // url: String
    // connections: Number
    // warmup: Number
    // duration: Number
    // headers: List<string> (Array)
    // version: String
    // quiet: Boolean
}

function start(handler, requests) {

    // Invoked before the benchmark is started. Once per thread.
    // handler: System.Net.Http.SocketsHttpHandler
    // requests: List<System.Net.Http.HttpRequestMessage> (Array)

}

function request(request, warmup) {

    // Invoked when a request is created.
    // request: System.Net.Http.HttpRequestMessage
    // warmup: bool

    console.info(`request ${request.requestUri}`);
}

function response(response, warmup) {

    // Invoked when a request is created.
    // response: System.Net.Http.HttpResponseMessage
    // warmup: bool

    console.warn(`response ${response.statusCode}`);
}

function error(exception) {

    // Invoked when an error occurs
    // exception: System.Exception

}

function stop(handler) {

    // Invoked when a client is stopped.
    // handler: System.Net.Http.SocketsHttpHandler

}