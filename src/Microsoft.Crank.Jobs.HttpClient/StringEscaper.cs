// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Text;
using System.Text.RegularExpressions;

namespace Microsoft.Crank.Jobs.HttpClientClient
{
    /// <summary>
    /// Provides methods to unescape string literals, converting escape sequences like \x00, \0, \n, \r, \t to their byte equivalents.
    /// </summary>
    internal static partial class StringEscaper
    {
        /// <summary>
        /// Unescapes a string by converting escape sequences to their actual characters.
        /// Supports: \xHH (hex byte), \0 (null), \n (newline), \r (carriage return), \t (tab), \\ (backslash)
        /// </summary>
        public static string Unescape(string input)
        {
            if (string.IsNullOrEmpty(input))
            {
                return input;
            }

            return EscapeSequenceRegex().Replace(input, match =>
            {
                var value = match.Value;

                // Handle \xHH format (hex byte)
                if (value.StartsWith("\\x", StringComparison.Ordinal) && value.Length == 4)
                {
                    var hex = value.Substring(2);
                    if (byte.TryParse(hex, System.Globalization.NumberStyles.HexNumber, null, out var b))
                    {
                        return ((char)b).ToString();
                    }
                }

                // Handle named escapes
                return value switch
                {
                    "\\0" => "\0",
                    "\\n" => "\n",
                    "\\r" => "\r",
                    "\\t" => "\t",
                    "\\\\" => "\\",
                    _ => value // Leave unrecognized escapes as-is
                };
            });
        }

        // Match \xHH, \0, \n, \r, \t, or \\
        [GeneratedRegex(@"\\x[0-9A-Fa-f]{2}|\\[0nrt\\]")]
        private static partial Regex EscapeSequenceRegex();
    }
}
