// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text.Json;

namespace Microsoft.Crank.Jobs.HttpClientClient
{
    internal class TimelineFactory
    {
        public static Timeline[] FromHar(string filePath)
        {
            using (JsonDocument document = JsonDocument.Parse(File.ReadAllText(filePath)))
            {
                var logElement = document.RootElement.GetProperty("log");
                var entriesElement = logElement.GetProperty("entries");

                var previousStartedDateTime = DateTime.MinValue;
                var steps = new List<Timeline>();

                foreach (var entryElement in entriesElement.EnumerateArray())
                {
                    var startedDateTime = entryElement.GetProperty("startedDateTime").GetDateTime();

                    var delay = previousStartedDateTime == DateTime.MinValue || startedDateTime <= previousStartedDateTime
                        ? TimeSpan.Zero
                        : startedDateTime - previousStartedDateTime
                        ;

                    previousStartedDateTime = startedDateTime;

                    var requestElement = entryElement.GetProperty("request");

                    var step = new Timeline
                    {
                        Delay = delay,
                        Uri = new Uri(requestElement.GetProperty("url").GetString()),
                        Method = new HttpMethod(requestElement.GetProperty("method").GetString()),
                        Headers = new Dictionary<string, string>(requestElement.GetProperty("headers").EnumerateArray().Select(x => new KeyValuePair<string, string>(x.GetProperty("name").GetString(), x.GetProperty("value").GetString())))
                    };

                    if (requestElement.TryGetProperty("postData", out var postData))
                    {
                        step.HttpContent = new StringContent(postData.GetProperty("text").GetString(), System.Text.Encoding.UTF8, postData.GetProperty("mimeType").GetString());
                    }

                    steps.Add(step);
                }

                return steps.ToArray();
            }
        }

        public static Timeline[] FromUrls(string filePath)
        {
            var timelines = new List<Timeline>();

            foreach (var line in File.ReadAllLines(filePath))
            {
                timelines.Add(new Timeline { Uri = new Uri(line), Method = HttpMethod.Get });
            }

            return timelines.ToArray();
        }
    }
}
