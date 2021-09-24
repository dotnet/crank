// Add custom requests dynamically

function start(handler, requests) {
    requests.Clear();
    requests.Add(new System.Net.Http.HttpRequestMessage(System.Net.Http.HttpMethod.Get, "https://orchardcore.net"));
}

function request(request, warmup) {
    console.warn(`url: ${request.requestUri}`);
}
