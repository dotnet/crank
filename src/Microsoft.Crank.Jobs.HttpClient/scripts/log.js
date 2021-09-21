// Logs all request and their result

function request(request, warmup) {
    console.info(`url: ${request.requestUri}`);
}

function response(response, warmup) {
    console.warn(`status: ${response.statusCode}`);
}
