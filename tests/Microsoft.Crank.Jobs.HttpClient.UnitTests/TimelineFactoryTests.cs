using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text.Json;

namespace Microsoft.Crank.Jobs.HttpClientClient.UnitTests
{
    /// <summary>
    /// Unit tests for the <see cref="TimelineFactory"/> class.
    /// </summary>
    [TestClass]
    public class TimelineFactoryTests
    {
        private readonly string _harFilePath;
        private readonly string _urlsFilePath;

        public TimelineFactoryTests()
        {
            _harFilePath = "test.har";
            _urlsFilePath = "test.urls";
        }

        /// <summary>
        /// Tests the <see cref="TimelineFactory.FromHar(string)"/> method to ensure it correctly parses a HAR file.
        /// </summary>
        [TestMethod]
        public void FromHar_ValidHarFile_ReturnsCorrectTimelines()
        {
            // Arrange
            var harContent = @"
            {
                ""log"": {
                    ""entries"": [
                        {
                            ""startedDateTime"": ""2023-01-01T00:00:00Z"",
                            ""request"": {
                                ""url"": ""http://example.com"",
                                ""method"": ""GET"",
                                ""headers"": [
                                    { ""name"": ""Accept"", ""value"": ""*/*"" }
                                ]
                            }
                        }
                    ]
                }
            }";
            File.WriteAllText(_harFilePath, harContent);

            // Act
            var timelines = TimelineFactory.FromHar(_harFilePath);

            // Assert
            Assert.AreEqual(1, timelines.Length);
            Assert.AreEqual(new Uri("http://example.com"), timelines[0].Uri);
            Assert.AreEqual(HttpMethod.Get, timelines[0].Method);
            Assert.AreEqual(TimeSpan.Zero, timelines[0].Delay);
            Assert.AreEqual("*/*", timelines[0].Headers["Accept"]);
        }

        /// <summary>
        /// Tests the <see cref="TimelineFactory.FromHar(string)"/> method to ensure it throws an exception for an invalid HAR file.
        /// </summary>
        [TestMethod]
        [ExpectedException(typeof(JsonException))]
        public void FromHar_InvalidHarFile_ThrowsJsonException()
        {
            // Arrange
            var invalidHarContent = @"{ ""log"": { ""entries"": [ { ""startedDateTime"": ""invalid-date"" } ] } }";
            File.WriteAllText(_harFilePath, invalidHarContent);

            // Act
            TimelineFactory.FromHar(_harFilePath);
        }

        /// <summary>
        /// Tests the <see cref="TimelineFactory.FromUrls(string)"/> method to ensure it correctly parses a URLs file.
        /// </summary>
        [TestMethod]
        public void FromUrls_ValidUrlsFile_ReturnsCorrectTimelines()
        {
            // Arrange
            var urlsContent = "http://example.com\nhttp://example.org";
            File.WriteAllText(_urlsFilePath, urlsContent);

            // Act
            var timelines = TimelineFactory.FromUrls(_urlsFilePath);

            // Assert
            Assert.AreEqual(2, timelines.Length);
            Assert.AreEqual(new Uri("http://example.com"), timelines[0].Uri);
            Assert.AreEqual(HttpMethod.Get, timelines[0].Method);
            Assert.AreEqual(new Uri("http://example.org"), timelines[1].Uri);
            Assert.AreEqual(HttpMethod.Get, timelines[1].Method);
        }

        /// <summary>
        /// Tests the <see cref="TimelineFactory.FromUrls(string)"/> method to ensure it throws an exception for an invalid URLs file.
        /// </summary>
        [TestMethod]
        [ExpectedException(typeof(UriFormatException))]
        public void FromUrls_InvalidUrlsFile_ThrowsUriFormatException()
        {
            // Arrange
            var invalidUrlsContent = "invalid-url";
            File.WriteAllText(_urlsFilePath, invalidUrlsContent);

            // Act
            TimelineFactory.FromUrls(_urlsFilePath);
        }
    }
}
