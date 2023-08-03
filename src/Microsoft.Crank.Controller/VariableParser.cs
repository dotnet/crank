// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Globalization;
using McMaster.Extensions.CommandLineUtils.Abstractions;
using Newtonsoft.Json.Linq;

namespace Microsoft.Crank.Controller
{
    public class VariableParser : IValueParser
    {
        public Type TargetType { get; } = typeof(ValueTuple<string, JToken>);

        public object Parse(string argName, string value, CultureInfo culture)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return default(ValueTuple<string, JToken>);
            }

            var fragments = value!.Split('=');

            try
            {
                // variable key
                var variableKey = fragments[0].Trim();

                // variable value, a json format value, such as json object, array, number, etc.
                var variableValue = JToken.Parse(fragments[1].Trim());

                return (variableKey, variableValue);
            }
            catch (Exception ex)
            {
                throw new FormatException(
                $"Invalid {argName} argument: '{value}', format is \"[NAME]=[VALUE]\"",
                ex);
            }
        }
    }
}
