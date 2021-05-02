// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Azure.Messaging.ServiceBus;

namespace Microsoft.Crank.AzureDevOpsWorker
{
    public class DevopsMessage
    {
        public enum ResultTypes
        {
            Succeeded,
            SucceededWithIssues,
            Skipped,
            Failed
        }

        private static readonly HttpClient _httpClient = new HttpClient();

        public string PlanUrl { get; set; }
        public string ProjectId { get; set; }
        public string HubName { get; set; }
        public string PlanId { get; set; }
        public string JobId { get; set; }
        public string TimelineId { get; set; }
        public string TaskInstanceName { get; set; }
        public string TaskInstanceId { get; set; }
        public string AuthToken { get; set; }

        public Records Records { get; set; }
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

            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic",
                    Convert.ToBase64String(Encoding.ASCII.GetBytes(":" + AuthToken)));
        }

        public async Task<bool> SendTaskStartedEventAsync()
        {
            var taskStartedEventUrl = $"{PlanUrl}/{ProjectId}/_apis/distributedtask/hubs/{HubName}/plans/{PlanId}/events?api-version=2.0-preview.1";

            var body = new
            {
                name = "TaskStarted",
                taskId = TaskInstanceId,
                jobId = JobId
            };

            var requestBody = JsonSerializer.Serialize(body);

            try
            {
                var result = await PostDataAsync(taskStartedEventUrl, requestBody);
                return result.IsSuccessStatusCode;
            }
            catch (Exception e)
            {
                Console.WriteLine($"SendTaskStartedEventAsync failed: {taskStartedEventUrl}");
                Console.WriteLine(e.ToString());
                return false;
            }
        }
        
        public async Task<bool> SendTaskCompletedEventAsync(ResultTypes resultType)
        {
            var taskCompletedEventUrl = $"{PlanUrl}/{ProjectId}/_apis/distributedtask/hubs/{HubName}/plans/{PlanId}/events?api-version=2.0-preview.1";

            var body = new
            {
                name = "TaskCompleted",
                taskId = TaskInstanceId,
                jobId = JobId,
                result = resultType.ToString().ToLowerInvariant()
            };
            
            var requestBody = JsonSerializer.Serialize(body);

            try
            {
                var result = await PostDataAsync(taskCompletedEventUrl, requestBody);
                return result.IsSuccessStatusCode;
            }
            catch (Exception e)
            {
                Console.WriteLine($"SendTaskCompletedEventAsync failed: {taskCompletedEventUrl}");
                Console.WriteLine(e.ToString());
                return false;
            }        
        }

        public async Task<bool> SendTaskLogFeedsAsync(string message)
        {
            var taskLogFeedsUrl = $"{PlanUrl}/{ProjectId}/_apis/distributedtask/hubs/{HubName}/plans/{PlanId}/timelines/{TimelineId}/records/{JobId}/feed?api-version=4.1";

            var body = new
            {
                value = new string[] { message },
                count = 1
            };

            var requestBody = JsonSerializer.Serialize(body);

            try
            {
                var result = await PostDataAsync(taskLogFeedsUrl, requestBody);
                return result.IsSuccessStatusCode;
            }
            catch (Exception e)
            {
                Console.WriteLine($"SendTaskLogFeedsAsync failed: {taskLogFeedsUrl}");
                Console.WriteLine(e.ToString());
                return false;
            }
        }

        public async Task<string> CreateTaskLogAsync()
        {
            var taskLogCreateUrl = $"{PlanUrl}/{ProjectId}/_apis/distributedtask/hubs/{HubName}/plans/{PlanId}/logs?api-version=4.1";

            var body = new
            {
                path = String.Format(@"logs\{0:D}", TaskInstanceId),
            };

            var requestBody = JsonSerializer.Serialize(body);

            try
            {
                var result = await PostDataAsync(taskLogCreateUrl, requestBody);
                if (result.IsSuccessStatusCode)
                {
                    return await result.Content.ReadAsStringAsync();
                }
                else
                {
                    return null;
                }
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

        public async Task<bool> UpdateTaskTimelineRecordAsync(string taskLogObject)
        {
            var updateTimelineUrl = $"{PlanUrl}/{ProjectId}/_apis/distributedtask/hubs/{HubName}/plans/{PlanId}/timelines/{TimelineId}/records?api-version=4.1";

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

            var requestBody = JsonSerializer.Serialize(body);

            try
            {
                var result = await PatchDataAsync(updateTimelineUrl, requestBody);
                return result.IsSuccessStatusCode;
            }
            catch (Exception e)
            {
                Console.WriteLine($"UpdateTaskTimelineRecordAsync failed: {updateTimelineUrl}");
                Console.WriteLine(e.ToString());
                return false;
            }
        }

        public async Task<Records> GetRecordsAsync()
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

        private async Task<HttpResponseMessage> PostDataAsync(string url, HttpContent content)
        {
            var response = await _httpClient.PostAsync(new Uri(url), content);

            return response;
        }

        private async Task<HttpResponseMessage> GetDataAsync(string url)
        {
            var response = await _httpClient.GetAsync(new Uri(url));

            return response;
        }

        private async Task<HttpResponseMessage> PostDataAsync(string url, string requestBody)
        {
            var buffer = Encoding.UTF8.GetBytes(requestBody);
            var byteContent = new ByteArrayContent(buffer);
            byteContent.Headers.ContentType = new MediaTypeHeaderValue("application/json");

            var response = await _httpClient.PostAsync(new Uri(url), byteContent);

            return response;
        }

        private async Task<HttpResponseMessage> PatchDataAsync(string url, string requestBody)
        {
            var buffer = Encoding.UTF8.GetBytes(requestBody);
            var byteContent = new ByteArrayContent(buffer);
            byteContent.Headers.ContentType = new MediaTypeHeaderValue("application/json");

            var response = await _httpClient.PatchAsync(new Uri(url), byteContent);

            return response;
        }
    }
}
