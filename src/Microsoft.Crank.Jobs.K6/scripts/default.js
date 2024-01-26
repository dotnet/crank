import http from 'k6/http';
import { textSummary } from 'https://jslib.k6.io/k6-summary/0.1.0/index.js';

const url = __ENV.URL;
const presetHeaders = __ENV.HEADERS

export const options = {
    summaryTrendStats: ['avg', 'min', 'max', 'p(50)', 'p(75)', 'p(90)', 'p(95)', 'p(99)']
};

export default function () {

    var headers = {
        none: {},
        plaintext: { 'Accept': 'text/plain,text/html;q=0.9,application/xhtml+xml;q=0.9,application/xml;q=0.8,*/*;q=0.7', 'Connection': 'keep-alive' },
        html: { 'Accept': 'text/html,application/xhtml+xml,application/xml;q=0.9,*/*;q=0.8', 'Connection': 'keep-alive' },
        json: { 'Accept': 'application/json,text/html;q=0.9,application/xhtml+xml;q=0.9,application/xml;q=0.8,*/*;q=0.7', 'Connection': 'keep-alive' },
        connectionclose: { 'Connection': 'close' },
    }

    const params = {
        // c.f. https://grafana.com/docs/k6/latest/javascript-api/k6-http/params/
        headers: headers[presetHeaders]
    };

    http.get(url, params);
  // console.log(url);
}

export function handleSummary(data) {
    // c.f. https://k6.io/docs/results-output/end-of-test/custom-summary/
    return {
        'stdout': textSummary(data, { indent: ' ', enableColors: false }), // Show the text summary to stdout...
        'summary.json': JSON.stringify(data) // and saves the raw data as JSON in a file
    };
}
