// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Microsoft.Crank.AzureDevOpsWorker
{
    public sealed class TimeSpanConverter : JsonConverter<TimeSpan>
    {
        public override TimeSpan Read(
            ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) =>
            TimeSpan.Parse(reader.GetString() ?? "0");

        public override void Write(
            Utf8JsonWriter writer, TimeSpan value, JsonSerializerOptions options) =>
            writer.WriteStringValue(value.ToString());
    }
}
