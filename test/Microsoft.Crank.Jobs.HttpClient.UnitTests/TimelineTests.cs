using System;
using System.Collections.Generic;
using System.Net.Http;
using Microsoft.Crank.Jobs.HttpClientClient;
using Xunit;

namespace Microsoft.Crank.Jobs.HttpClientClient.UnitTests
{
    /// <summary>
    /// Unit tests for the <see cref="Timeline"/> class.
    /// </summary>
    public class TimelineTests
    {
        private readonly Timeline _timeline;

        /// <summary>
        /// Initializes a new instance of the <see cref="TimelineTests"/> class.
        /// </summary>
        public TimelineTests()
        {
            _timeline = new Timeline();
        }

        /// <summary>
        /// Tests that the default Headers property is initialized to a non-null empty dictionary upon instantiation.
        /// </summary>
        [Fact]
        public void Constructor_InitializesHeadersDictionary()
        {
            // Assert
            Assert.NotNull(_timeline.Headers);
            Assert.Empty(_timeline.Headers);
        }

        /// <summary>
        /// Tests that the Uri property can be set and retrieved correctly with a valid Uri.
        /// </summary>
        [Fact]
        public void Uri_SetAndGet_ExpectedValue()
        {
            // Arrange
            Uri expectedUri = new Uri("https://example.com");

            // Act
            _timeline.Uri = expectedUri;
            Uri actualUri = _timeline.Uri;

            // Assert
            Assert.Equal(expectedUri, actualUri);
        }

        /// <summary>
        /// Tests that setting the Uri property to null and retrieving it returns null.
        /// </summary>
        [Fact]
        public void Uri_SetToNull_RetrievesNull()
        {
            // Act
            _timeline.Uri = null;
            Uri actualUri = _timeline.Uri;

            // Assert
            Assert.Null(actualUri);
        }

        /// <summary>
        /// Tests that the Delay property can be set and retrieved correctly.
        /// </summary>
        [Fact]
        public void Delay_SetAndGet_ExpectedValue()
        {
            // Arrange
            TimeSpan expectedDelay = TimeSpan.FromSeconds(5);

            // Act
            _timeline.Delay = expectedDelay;
            TimeSpan actualDelay = _timeline.Delay;

            // Assert
            Assert.Equal(expectedDelay, actualDelay);
        }

        /// <summary>
        /// Tests that the Method property can be set and retrieved correctly with a valid HttpMethod.
        /// </summary>
        [Fact]
        public void Method_SetAndGet_ExpectedValue()
        {
            // Arrange
            HttpMethod expectedMethod = HttpMethod.Post;

            // Act
            _timeline.Method = expectedMethod;
            HttpMethod actualMethod = _timeline.Method;

            // Assert
            Assert.Equal(expectedMethod, actualMethod);
        }

        /// <summary>
        /// Tests that the Headers property can be assigned a new dictionary and retains the assigned key-value pairs.
        /// </summary>
        [Fact]
        public void Headers_SetAndGet_ExpectedValue()
        {
            // Arrange
            var expectedHeaders = new Dictionary<string, string>
            {
                {"Content-Type", "application/json"},
                {"Authorization", "Bearer token"}
            };

            // Act
            _timeline.Headers = expectedHeaders;
            Dictionary<string, string> actualHeaders = _timeline.Headers;

            // Assert
            Assert.Equal(expectedHeaders, actualHeaders);
        }

        /// <summary>
        /// Tests that the Headers property can be set to null and retrieved as null.
        /// </summary>
        [Fact]
        public void Headers_SetToNull_RetrievesNull()
        {
            // Act
            _timeline.Headers = null;
            Dictionary<string, string> actualHeaders = _timeline.Headers;

            // Assert
            Assert.Null(actualHeaders);
        }

        /// <summary>
        /// Tests that the HttpContent property can be set and retrieved correctly using a valid StringContent instance.
        /// </summary>
        [Fact]
        public void HttpContent_SetAndGet_ExpectedValue()
        {
            // Arrange
            HttpContent expectedContent = new StringContent("Test content");

            // Act
            _timeline.HttpContent = expectedContent;
            HttpContent actualContent = _timeline.HttpContent;

            // Assert
            Assert.Equal(expectedContent, actualContent);
        }

        /// <summary>
        /// Tests that the HttpContent property can be set to null and retrieved as null.
        /// </summary>
        [Fact]
        public void HttpContent_SetToNull_RetrievesNull()
        {
            // Act
            _timeline.HttpContent = null;
            HttpContent actualContent = _timeline.HttpContent;

            // Assert
            Assert.Null(actualContent);
        }

        /// <summary>
        /// Tests that the MimeType property can be set and retrieved correctly with a valid MIME type string.
        /// </summary>
        [Fact]
        public void MimeType_SetAndGet_ExpectedValue()
        {
            // Arrange
            string expectedMimeType = "application/json";

            // Act
            _timeline.MimeType = expectedMimeType;
            string actualMimeType = _timeline.MimeType;

            // Assert
            Assert.Equal(expectedMimeType, actualMimeType);
        }

        /// <summary>
        /// Tests that the MimeType property can be set to null and retrieved as null.
        /// </summary>
        [Fact]
        public void MimeType_SetToNull_RetrievesNull()
        {
            // Act
            _timeline.MimeType = null;
            string actualMimeType = _timeline.MimeType;

            // Assert
            Assert.Null(actualMimeType);
        }
    }
}
