// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Text;
using Microsoft.Crank.AzureDevOpsWorker;
using Xunit;

namespace Microsoft.Crank.AzureDevOpsWorker.UnitTests
{
    /// <summary>
    /// Unit tests for the <see cref="JobPayload"/> class.
    /// </summary>
    public class JobPayloadTests
    {
        private readonly TimeSpan _defaultTimeout = TimeSpan.FromMinutes(10);

        /// <summary>
        /// Tests that the Deserialize method correctly returns a JobPayload instance when provided with valid JSON without any preamble.
        /// </summary>
        [Fact]
        public void Deserialize_ValidJsonWithoutPreamble_ReturnsCorrectJobPayload()
        {
            // Arrange
            string json = "{\"name\":\"TestJob\",\"args\":[\"arg1\",\"arg2\"],\"retries\":3,\"condition\":\"true\"}";
            byte[] data = Encoding.UTF8.GetBytes(json);

            // Act
            JobPayload result = JobPayload.Deserialize(data);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("TestJob", result.Name);
            Assert.Equal(new string[] { "arg1", "arg2" }, result.Args);
            Assert.Equal(3, result.Retries);
            Assert.Equal("true", result.Condition);
            Assert.Equal(_defaultTimeout, result.Timeout);
        }

        /// <summary>
        /// Tests that the Deserialize method correctly extracts and deserializes JSON when the input contains a preamble and extraneous characters after the JSON document.
        /// </summary>
        [Fact]
        public void Deserialize_ValidJsonWithPreamble_ReturnsCorrectJobPayload()
        {
            // Arrange
            string preamble = "RandomPreambleText123";
            string json = "{\"name\":\"PreambleJob\",\"args\":[\"a1\",\"a2\"],\"retries\":2,\"condition\":\"check\"}";
            string extra = "ExtraInvalidChar";
            string combined = preamble + " " + json + extra;
            byte[] data = Encoding.UTF8.GetBytes(combined);

            // Act
            JobPayload result = JobPayload.Deserialize(data);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("PreambleJob", result.Name);
            Assert.Equal(new string[] { "a1", "a2" }, result.Args);
            Assert.Equal(2, result.Retries);
            Assert.Equal("check", result.Condition);
            Assert.Equal(_defaultTimeout, result.Timeout);
        }

        /// <summary>
        /// Tests that the Deserialize method throws an exception when no JSON document marker is found in the provided data.
        /// </summary>
        /// <param name="input">A string without a JSON document marker.</param>
        [Theory]
        [InlineData("No JSON content here")]
        [InlineData("")]
        public void Deserialize_NoJsonMarker_ThrowsException(string input)
        {
            // Arrange
            byte[] data = Encoding.UTF8.GetBytes(input);

            // Act & Assert
            Exception ex = Assert.Throws<Exception>(() => JobPayload.Deserialize(data));
            Assert.Contains("Couldn't find beginning of JSON document", ex.InnerException?.Message);
            Assert.Contains(Convert.ToHexString(data), ex.Message);
        }

        /// <summary>
        /// Tests that the Deserialize method throws an exception with an inner JsonException when the JSON document is malformed.
        /// </summary>
        [Fact]
        public void Deserialize_InvalidJson_ThrowsException()
        {
            // Arrange
            // Create a malformed JSON string missing a closing brace.
            string invalidJson = "{\"name\":\"InvalidJob\",\"args\":[\"arg1\"]";
            byte[] data = Encoding.UTF8.GetBytes(invalidJson);

            // Act & Assert
            Exception ex = Assert.Throws<Exception>(() => JobPayload.Deserialize(data));
            Assert.NotNull(ex.InnerException);
            Assert.IsType<System.Text.Json.JsonException>(ex.InnerException);
            Assert.Contains(Convert.ToHexString(data), ex.Message);
        }
    }
}
