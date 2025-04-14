using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using Microsoft.Crank.Jobs.HttpClientClient;
using Xunit;

namespace Microsoft.Crank.Jobs.HttpClientClient.UnitTests
{
    /// <summary>
    /// Unit tests for the <see cref="TimelineFactory"/> class.
    /// </summary>
    public class TimelineFactoryTests
    {
        /// <summary>
        /// Helper method to create a temporary file with given content.
        /// </summary>
        /// <param name="content">The file content to write.</param>
        /// <returns>The path to the temporary file created.</returns>
        private string CreateTempFileWithContent(string content)
        {
            string tempFile = Path.GetTempFileName();
            File.WriteAllText(tempFile, content);
            return tempFile;
        }

        /// <summary>
        /// Tests the <see cref="TimelineFactory.FromHar(string)"/> method with a valid HAR JSON
        /// containing multiple entries without postData. It verifies that the returned Timeline array 
        /// has the correct URIs, HTTP methods, headers, and delay calculations.
        /// </summary>
        [Fact]
        public void FromHar_ValidHarJsonWithoutPostData_ReturnsCorrectTimelines()
        {
            // Arrange
            DateTime time1 = DateTime.UtcNow;
            DateTime time2 = time1.AddSeconds(2);
            string harJson = $@"
{{
    ""log"": {{
        ""entries"": [
            {{
                ""startedDateTime"": ""{time1:o}"",
                ""request"": {{
                    ""url"": ""http://example.com/1"",
                    ""method"": ""GET"",
                    ""headers"": [{{ ""name"": ""Accept"", ""value"": ""application/json"" }}]
                }}
            }},
            {{
                ""startedDateTime"": ""{time2:o}"",
                ""request"": {{
                    ""url"": ""http://example.com/2"",
                    ""method"": ""POST"",
                    ""headers"": [{{ ""name"": ""Content-Type"", ""value"": ""application/json"" }}]
                }}
            }}
        ]
    }}
}}";
            string tempFile = CreateTempFileWithContent(harJson);

            try
            {
                // Act
                Timeline[] timelines = TimelineFactory.FromHar(tempFile);

                // Assert
                Assert.Equal(2, timelines.Length);

                // Validate first timeline entry
                Timeline firstEntry = timelines[0];
                Assert.Equal(new Uri("http://example.com/1"), firstEntry.Uri);
                Assert.Equal(HttpMethod.Get, firstEntry.Method);
                Assert.NotNull(firstEntry.Headers);
                Assert.Single(firstEntry.Headers);
                Assert.Contains("Accept", firstEntry.Headers.Keys);
                Assert.Equal("application/json", firstEntry.Headers["Accept"]);
                Assert.Equal(TimeSpan.Zero, firstEntry.Delay);
                Assert.Null(firstEntry.HttpContent);

                // Validate second timeline entry
                Timeline secondEntry = timelines[1];
                Assert.Equal(new Uri("http://example.com/2"), secondEntry.Uri);
                Assert.Equal(HttpMethod.Post, secondEntry.Method);
                Assert.NotNull(secondEntry.Headers);
                Assert.Single(secondEntry.Headers);
                Assert.Contains("Content-Type", secondEntry.Headers.Keys);
                Assert.Equal("application/json", secondEntry.Headers["Content-Type"]);
                Assert.Equal(time2 - time1, secondEntry.Delay);
                Assert.Null(secondEntry.HttpContent);
            }
            finally
            {
                // Cleanup temporary file
                File.Delete(tempFile);
            }
        }

        /// <summary>
        /// Tests the <see cref="TimelineFactory.FromHar(string)"/> method with a valid HAR JSON 
        /// that includes an entry with postData. It verifies that the Timeline object correctly sets the HttpContent.
        /// </summary>
        [Fact]
        public void FromHar_ValidHarJsonWithPostData_ReturnsTimelineWithHttpContent()
        {
            // Arrange
            DateTime time = DateTime.UtcNow;
            string postDataText = "example body";
            string mimeType = "text/plain";
            string harJson = $@"
{{
    ""log"": {{
        ""entries"": [
            {{
                ""startedDateTime"": ""{time:o}"",
                ""request"": {{
                    ""url"": ""http://example.com/post"",
                    ""method"": ""PUT"",
                    ""headers"": [{{ ""name"": ""X-Test"", ""value"": ""true"" }}],
                    ""postData"": {{
                        ""text"": ""{postDataText}"",
                        ""mimeType"": ""{mimeType}""
                    }}
                }}
            }}
        ]
    }}
}}";
            string tempFile = CreateTempFileWithContent(harJson);

            try
            {
                // Act
                Timeline[] timelines = TimelineFactory.FromHar(tempFile);

                // Assert
                Assert.Single(timelines);
                Timeline timeline = timelines[0];
                Assert.Equal(new Uri("http://example.com/post"), timeline.Uri);
                Assert.Equal(new HttpMethod("PUT"), timeline.Method);
                Assert.NotNull(timeline.Headers);
                Assert.Single(timeline.Headers);
                Assert.Contains("X-Test", timeline.Headers.Keys);
                Assert.Equal("true", timeline.Headers["X-Test"]);
                Assert.Equal(TimeSpan.Zero, timeline.Delay);
                Assert.NotNull(timeline.HttpContent);
                // Validate HttpContent by reading its content string
                string contentString = timeline.HttpContent.ReadAsStringAsync().Result;
                Assert.Equal(postDataText, contentString);
            }
            finally
            {
                // Cleanup temporary file
                File.Delete(tempFile);
            }
        }

        /// <summary>
        /// Tests the <see cref="TimelineFactory.FromHar(string)"/> method when provided with a non-existent file.
        /// It verifies that a <see cref="FileNotFoundException"/> is thrown.
        /// </summary>
        [Fact]
        public void FromHar_NonExistentFile_ThrowsFileNotFoundException()
        {
            // Arrange
            string nonExistentPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString() + ".json");

            // Act & Assert
            Assert.Throws<FileNotFoundException>(() => TimelineFactory.FromHar(nonExistentPath));
        }

        /// <summary>
        /// Tests the <see cref="TimelineFactory.FromHar(string)"/> method with an invalid JSON file.
        /// It verifies that a <see cref="JsonException"/> is thrown due to invalid JSON format.
        /// </summary>
        [Fact]
        public void FromHar_InvalidJson_ThrowsJsonException()
        {
            // Arrange
            string invalidJson = "This is not a valid JSON";
            string tempFile = CreateTempFileWithContent(invalidJson);

            try
            {
                // Act & Assert
                Assert.Throws<JsonException>(() => TimelineFactory.FromHar(tempFile));
            }
            finally
            {
                // Cleanup temporary file
                File.Delete(tempFile);
            }
        }

        /// <summary>
        /// Tests the <see cref="TimelineFactory.FromUrls(string)"/> method with a valid file containing URLs.
        /// It verifies that the returned Timeline objects have the correct URI and default HttpMethod.Get.
        /// </summary>
        [Fact]
        public void FromUrls_ValidUrlsFile_ReturnsCorrectTimelines()
        {
            // Arrange
            string[] urls = new string[]
            {
                "http://example.com/1",
                "https://example.com/2"
            };
            string content = string.Join(Environment.NewLine, urls);
            string tempFile = CreateTempFileWithContent(content);

            try
            {
                // Act
                Timeline[] timelines = TimelineFactory.FromUrls(tempFile);

                // Assert
                Assert.Equal(urls.Length, timelines.Length);
                for (int i = 0; i < urls.Length; i++)
                {
                    Assert.Equal(new Uri(urls[i]), timelines[i].Uri);
                    Assert.Equal(HttpMethod.Get, timelines[i].Method);
                    // For FromUrls, Delay is default, and Headers and HttpContent should be null.
                    Assert.Equal(default(TimeSpan), timelines[i].Delay);
                    Assert.Null(timelines[i].Headers);
                    Assert.Null(timelines[i].HttpContent);
                }
            }
            finally
            {
                // Cleanup temporary file
                File.Delete(tempFile);
            }
        }

        /// <summary>
        /// Tests the <see cref="TimelineFactory.FromUrls(string)"/> method when provided with a non-existent file.
        /// It verifies that a <see cref="FileNotFoundException"/> is thrown.
        /// </summary>
        [Fact]
        public void FromUrls_NonExistentFile_ThrowsFileNotFoundException()
        {
            // Arrange
            string nonExistentPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString() + ".txt");

            // Act & Assert
            Assert.Throws<FileNotFoundException>(() => TimelineFactory.FromUrls(nonExistentPath));
        }

        /// <summary>
        /// Tests the <see cref="TimelineFactory.FromUrls(string)"/> method with a file containing an invalid URL.
        /// It verifies that a <see cref="UriFormatException"/> is thrown when parsing an invalid URL.
        /// </summary>
        [Fact]
        public void FromUrls_InvalidUrl_ThrowsUriFormatException()
        {
            // Arrange
            string[] lines = new string[]
            {
                "http://example.com/valid",
                "invalid_url"
            };
            string content = string.Join(Environment.NewLine, lines);
            string tempFile = CreateTempFileWithContent(content);

            try
            {
                // Act & Assert
                Assert.Throws<UriFormatException>(() => TimelineFactory.FromUrls(tempFile));
            }
            finally
            {
                // Cleanup temporary file
                File.Delete(tempFile);
            }
        }
    }
}
