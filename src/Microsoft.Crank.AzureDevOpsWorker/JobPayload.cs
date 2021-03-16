// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Microsoft.Crank.AzureDevOpsWorker
{

    public sealed class JobPayload
    {
        private static readonly TimeSpan DefaultJobTimeout = TimeSpan.FromMinutes(10);

        [JsonConverter(typeof(TimeSpanConverter))]
        public TimeSpan Timeout { get; set; } = DefaultJobTimeout;

        public string Name { get; set; } = null!;
        public string[] Args { get; set; } = null!;

        public string RawPayload { get; set; } = null!;

        public static JobPayload Deserialize(byte[] data)
        {
            try
            {
                var str = Encoding.UTF8.GetString(data);

                // Azure Devops adds a DataContractSerializer preamble to the message, and also
                // an invalid JSON char at the end of the message
                str = str[str.IndexOf("{")..];
                str = str.Substring(0, str.LastIndexOf("}") + 1);

                var result = JsonSerializer.Deserialize<JobPayload>(
                    str, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                result!.RawPayload = str;

                return result;
            }
            catch (Exception ex)
            {
                throw new Exception(
                    $"Error while parsing message body: {Encoding.UTF8.GetString(data)}", ex);
            }
        }
    }
}
