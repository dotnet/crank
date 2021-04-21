// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Azure.Messaging.ServiceBus;

namespace Microsoft.Crank.AzureDevOpsWorker
{
    public sealed class DevopsMessage
    {
        private static readonly HttpClient _httpClient = new();

        public string PlanUrl { get; set; }
        public string ProjectId { get; set; }
        public string HubName { get; set; }
        public string PlanId { get; set; }
        public string JobId { get; set; }
        public string TimelineId { get; set; }
        public string TaskInstanceName { get; set; }
        public string TaskInstanceId { get; set; }
        public string AuthToken { get; set; }
        public Records? Records { get; set; }

        private DateTime _lastRecordsRefresh = DateTime.UtcNow;
        private static readonly JsonSerializerOptions _serializationOptions = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

    public DevopsMessage(ServiceBusReceivedMessage message)
        {
            PlanUrl = (string)message.ApplicationProperties["PlanUrl"];
            ProjectId = (string)message.ApplicationProperties["ProjectId"];
            HubName = (string)message.ApplicationProperties["HubName"];
            PlanId = (string)message.ApplicationProperties["PlanId"];
            JobId = (string)message.ApplicationProperties["JobId"];
            TimelineId = (string)message.ApplicationProperties["TimelineId"];
            TaskInstanceName = (string)message.ApplicationProperties["TaskInstanceName"];
            TaskInstanceId = (string)message.ApplicationProperties["TaskInstanceId"];
            AuthToken = (string)message.ApplicationProperties["AuthToken"];

            _httpClient.DefaultRequestHeaders.Authorization =
                new("Basic", Convert.ToBase64String(Encoding.ASCII.GetBytes($":{AuthToken}")));
        }

        public Task<bool> SendTaskStartedEventAsync()
        {
            var taskStartedEventUrl =
                $"{PlanUrl}/{ProjectId}/_apis/distributedtask/hubs/{HubName}/plans/{PlanId}/events?api-version=2.0-preview.1";

            var body = new
            {
                name = "TaskStarted",
                taskId = TaskInstanceId,
                jobId = JobId
            };

            return SendAsync(taskStartedEventUrl, body);
        }

        public Task<bool> SendTaskCompletedEventAsync(bool succeeded)
        {
            var taskCompletedEventUrl =
                $"{PlanUrl}/{ProjectId}/_apis/distributedtask/hubs/{HubName}/plans/{PlanId}/events?api-version=2.0-preview.1";

            var body = new
            {
                name = "TaskCompleted",
                taskId = TaskInstanceId,
                jobId = JobId,
                result = succeeded ? "succeeded" : "failed",
            };

            return SendAsync(taskCompletedEventUrl, body);
        }

        public Task<bool> SendTaskLogFeedsAsync(string message)
        {
            var taskLogFeedsUrl =
                $"{PlanUrl}/{ProjectId}/_apis/distributedtask/hubs/{HubName}/plans/{PlanId}/timelines/{TimelineId}/records/{JobId}/feed?api-version=4.1";

            var body = new
            {
                value = new string[] { message },
                count = 1
            };

            return SendAsync(taskLogFeedsUrl, body);
        }

        public async Task<string?> CreateTaskLogAsync()
        {
            var taskLogCreateUrl =
                $"{PlanUrl}/{ProjectId}/_apis/distributedtask/hubs/{HubName}/plans/{PlanId}/logs?api-version=4.1";

            var body = new
            {
                path = $@"logs\{TaskInstanceId:D}",
            };

            var requestBody = JsonSerializer.Serialize(body);

            try
            {
                var result = await PostDataAsync(taskLogCreateUrl, requestBody);
                return result.IsSuccessStatusCode
                    ? await result.Content.ReadAsStringAsync()
                    : null;
            }
            catch (Exception e)
            {
                Console.WriteLine($"CreateTaskLogAsync failed: {taskLogCreateUrl}");
                Console.WriteLine(e.ToString());
                return null;
            }
        }

        public async Task<bool> AppendToTaskLogAsync(string taskLogId, string message)
        {
            // Append to task log
            // url: {planUri}/{projectId}/_apis/distributedtask/hubs/{hubName}/plans/{planId}/logs/{taskLogId}?api-version=4.1
            // body: log messages stream data

            var appendLogContentUrl = $"{PlanUrl}/{ProjectId}/_apis/distributedtask/hubs/{HubName}/plans/{PlanId}/logs/{taskLogId}?api-version=4.1";

            var buffer = Encoding.UTF8.GetBytes(message);
            var byteContent = new ByteArrayContent(buffer);

            try
            {
                var result = await PostDataAsync(appendLogContentUrl, byteContent);
                return result.IsSuccessStatusCode;
            }
            catch (Exception e)
            {
                Console.WriteLine($"AppendToTaskLogAsync failed: {appendLogContentUrl}");
                Console.WriteLine(e.ToString());
                return false;
            }
        }

        public Task<bool> UpdateTaskTimelineRecordAsync(string taskLogObject)
        {
            var updateTimelineUrl =
                $"{PlanUrl}/{ProjectId}/_apis/distributedtask/hubs/{HubName}/plans/{PlanId}/timelines/{TimelineId}/records?api-version=4.1";

            var timelineRecord = new
            {
                id = TaskInstanceId,
                log = taskLogObject
            };

            var body = new
            {
                value = new[] { timelineRecord },
                count = 1
            };

            return SendAsync(updateTimelineUrl, body);
        }

        private async Task<bool> SendAsync<TBody>(
            string url, TBody body, [CallerMemberName] string callerName = "")
        {
            var requestBody = JsonSerializer.Serialize(body);

            try
            {
                var result = await PostDataAsync(url, requestBody);
                return result.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"{callerName} failed: {url}");
                Console.WriteLine(ex);
                return false;
            }
        }

        public async Task<Records?> GetRecordsAsync()
        {
            // NOTE: There is no API that allows to retrieve a single task details. Only the whole list.
            // So we cache the results to prevent rate limitting.

            var getRecordsUrl = $"{PlanUrl}/{ProjectId}/_apis/distributedtask/hubs/{HubName}/plans/{PlanId}/timelines/{TimelineId}/records?api-version=4.1";
            
            try
            {
                if (DateTime.UtcNow - _lastRecordsRefresh > TimeSpan.FromSeconds(60))
                {
                    _lastRecordsRefresh = DateTime.UtcNow;
                    return Records;
                }

                var result = await GetDataAsync(getRecordsUrl);

                if (result.IsSuccessStatusCode)
                {
                    var content = await result.Content.ReadAsStringAsync();

                    Records = JsonSerializer.Deserialize<Records>(content, _serializationOptions);
                }

                return Records;
            }
            catch (Exception e)
            {
                Console.WriteLine($"GetRecordsAsync failed: {getRecordsUrl}");
                Console.WriteLine(e.ToString());
                return null;
            }
        }

        private Task<HttpResponseMessage> PostDataAsync(string url, HttpContent content) =>
            _httpClient.PostAsync(new Uri(url), content);

        private Task<HttpResponseMessage> GetDataAsync(string url) =>
            _httpClient.GetAsync(new Uri(url));

        private async Task<HttpResponseMessage> PostDataAsync(string url, string requestBody)
        {
            var buffer = Encoding.UTF8.GetBytes(requestBody);
            using var byteContent = new ByteArrayContent(buffer);
            byteContent.Headers.ContentType = new MediaTypeHeaderValue("application/json");

            var response = await PostDataAsync(url, byteContent);

            return response;
        }
    }
}
