// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace Microsoft.Crank.IntegrationTests
{
    /// <summary>
    /// Unit tests for the StringEscaper utility class used by load generator clients.
    /// Tests are defined here with the expected behavior - the actual StringEscaper class
    /// is in the load generator projects (PipeliningClient, HttpClient).
    /// </summary>
    public class StringEscaperTests
    {
        [Fact]
        public void Unescape_NullByte_ReturnsNullCharacter()
        {
            var result = StringEscaper.Unescape(@"value\x00embedded");
            Assert.Equal("value\0embedded", result);
        }

        [Fact]
        public void Unescape_ShorthandNull_ReturnsNullCharacter()
        {
            var result = StringEscaper.Unescape(@"value\0end");
            Assert.Equal("value\0end", result);
        }

        [Fact]
        public void Unescape_NewlineAndCarriageReturn_ReturnsControlCharacters()
        {
            var result = StringEscaper.Unescape(@"line1\r\nline2");
            Assert.Equal("line1\r\nline2", result);
        }

        [Fact]
        public void Unescape_Tab_ReturnsTabCharacter()
        {
            var result = StringEscaper.Unescape(@"col1\tcol2");
            Assert.Equal("col1\tcol2", result);
        }

        [Fact]
        public void Unescape_Backslash_ReturnsSingleBackslash()
        {
            var result = StringEscaper.Unescape(@"path\\to\\file");
            Assert.Equal(@"path\to\file", result);
        }

        [Fact]
        public void Unescape_MultipleHexBytes_ReturnsAllBytes()
        {
            var result = StringEscaper.Unescape(@"X-Custom: value\x00\x0d\x0a");
            Assert.Equal("X-Custom: value\0\r\n", result);
        }

        [Fact]
        public void Unescape_NoEscapes_ReturnsOriginal()
        {
            var result = StringEscaper.Unescape("X-Custom: normal value");
            Assert.Equal("X-Custom: normal value", result);
        }

        [Fact]
        public void Unescape_NullInput_ReturnsNull()
        {
            var result = StringEscaper.Unescape(null);
            Assert.Null(result);
        }

        [Fact]
        public void Unescape_EmptyString_ReturnsEmpty()
        {
            var result = StringEscaper.Unescape("");
            Assert.Equal("", result);
        }

        [Fact]
        public void Unescape_UnrecognizedEscape_LeavesAsIs()
        {
            // \q is not a recognized escape, should remain as-is
            var result = StringEscaper.Unescape(@"test\qvalue");
            Assert.Equal(@"test\qvalue", result);
        }

        [Fact]
        public void Unescape_UppercaseHex_Works()
        {
            var result = StringEscaper.Unescape(@"\x0D\x0A");
            Assert.Equal("\r\n", result);
        }

        [Fact]
        public void Unescape_LowercaseHex_Works()
        {
            var result = StringEscaper.Unescape(@"\x0d\x0a");
            Assert.Equal("\r\n", result);
        }

        [Fact]
        public void Unescape_MixedEscapes_WorksTogether()
        {
            var result = StringEscaper.Unescape(@"start\x00middle\tend\n");
            Assert.Equal("start\0middle\tend\n", result);
        }

        // Local copy of StringEscaper for testing
        // This mirrors the implementation in the load generator projects
        private static class StringEscaper
        {
            public static string Unescape(string input)
            {
                if (string.IsNullOrEmpty(input))
                {
                    return input;
                }

                return System.Text.RegularExpressions.Regex.Replace(input, @"\\x[0-9A-Fa-f]{2}|\\[0nrt\\]", match =>
                {
                    var value = match.Value;

                    // Handle \xHH format (hex byte)
                    if (value.StartsWith("\\x", System.StringComparison.Ordinal) && value.Length == 4)
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
                        _ => value
                    };
                });
            }
        }
    }
}
