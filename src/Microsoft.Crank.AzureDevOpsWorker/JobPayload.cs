// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Text;
using System.Text.Json;

namespace Microsoft.Crank.AzureDevOpsWorker
{

    public class JobPayload
    {
        private static readonly TimeSpan DefaultJobTimeout = TimeSpan.FromMinutes(10);
        private static readonly JsonSerializerOptions _serializationOptions;

        static JobPayload()
        {
            _serializationOptions = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            _serializationOptions.Converters.Add(new TimeSpanConverter());
        }

        public TimeSpan Timeout { get; set; } = DefaultJobTimeout;

        public string Name { get; set; }
        public string[] Args { get; set; }

        // A JavaScript condition that must evaluate to true. "job" 
        public string Condition { get; set; }

        public static JobPayload Deserialize(byte[] data)
        {
            try
            {
                var str = Encoding.UTF8.GetString(data);

                // Azure Devops adds a DataContractSerializer preamble to the message, and also
                // an invalid JSON char at the end of the message

                // Example: @strin3http://schemas.microsoft.com/2003/10/Serialization/�{{ "name": "crank", ...

                var index = -1;
                do
                {
                    index = str.IndexOf('{', index + 1);

                    if (index == -1 || index >= str.Length)
                    {
                        throw new InvalidOperationException("Couldn't find beginning of JSON document.");
                    }
                }
                while (!char.IsWhiteSpace(str[index + 1]) && str[index + 1] != '\"');                
                
                str = str.Substring(index);
                str = str.Substring(0, str.LastIndexOf("}") + 1);
                var result = JsonSerializer.Deserialize<JobPayload>(str, _serializationOptions);

                return result;
            }
            catch (Exception e)
            {
                throw new Exception($"Error while parsing message body: {Convert.ToHexString(data)}", e);
            }
        }
    }
}
