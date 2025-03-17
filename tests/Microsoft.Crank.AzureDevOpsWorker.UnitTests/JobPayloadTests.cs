using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Text;
using System.Text.Json;

namespace Microsoft.Crank.AzureDevOpsWorker.UnitTests
{
    /// <summary>
    /// Unit tests for the <see cref="JobPayload"/> class.
    /// </summary>
    [TestClass]
    public class JobPayloadTests
    {
        private readonly JsonSerializerOptions _serializationOptions;

        public JobPayloadTests()
        {
            _serializationOptions = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            _serializationOptions.Converters.Add(new TimeSpanConverter());
        }

        /// <summary>
        /// Tests the <see cref="JobPayload.Deserialize(byte[])"/> method to ensure it correctly deserializes a valid byte array.
        /// </summary>
        [TestMethod]
        public void Deserialize_ValidByteArray_ReturnsJobPayload()
        {
            // Arrange
            var jobPayload = new JobPayload
            {
                Name = "test",
                Args = new[] { "arg1", "arg2" },
                Retries = 1,
                Condition = "true",
                Timeout = TimeSpan.FromMinutes(5)
            };
            var json = JsonSerializer.Serialize(jobPayload, _serializationOptions);
            var data = Encoding.UTF8.GetBytes(json);

            // Act
            var result = JobPayload.Deserialize(data);

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(jobPayload.Name, result.Name);
            Assert.AreEqual(jobPayload.Args.Length, result.Args.Length);
            Assert.AreEqual(jobPayload.Retries, result.Retries);
            Assert.AreEqual(jobPayload.Condition, result.Condition);
            Assert.AreEqual(jobPayload.Timeout, result.Timeout);
        }

        /// <summary>
        /// Tests the <see cref="JobPayload.Deserialize(byte[])"/> method to ensure it throws an exception for an invalid byte array.
        /// </summary>
        [TestMethod]
        public void Deserialize_InvalidByteArray_ThrowsException()
        {
            // Arrange
            var data = Encoding.UTF8.GetBytes("invalid json");

            // Act & Assert
            var exception = Assert.ThrowsException<Exception>(() => JobPayload.Deserialize(data));
            Assert.IsTrue(exception.Message.StartsWith("Error while parsing message body"));
        }

        /// <summary>
        /// Tests the <see cref="JobPayload.Deserialize(byte[])"/> method to ensure it throws an exception for a byte array with no JSON start.
        /// </summary>
        [TestMethod]
        public void Deserialize_NoJsonStart_ThrowsInvalidOperationException()
        {
            // Arrange
            var data = Encoding.UTF8.GetBytes("no json start");

            // Act & Assert
            var exception = Assert.ThrowsException<InvalidOperationException>(() => JobPayload.Deserialize(data));
            Assert.AreEqual("Couldn't find beginning of JSON document.", exception.Message);
        }

        /// <summary>
        /// Tests the <see cref="JobPayload.Deserialize(byte[])"/> method to ensure it correctly deserializes a byte array with Azure DevOps preamble.
        /// </summary>
        [TestMethod]
        public void Deserialize_ByteArrayWithPreamble_ReturnsJobPayload()
        {
            // Arrange
            var jobPayload = new JobPayload
            {
                Name = "test",
                Args = new[] { "arg1", "arg2" },
                Retries = 1,
                Condition = "true",
                Timeout = TimeSpan.FromMinutes(5)
            };
            var json = JsonSerializer.Serialize(jobPayload, _serializationOptions);
            var data = Encoding.UTF8.GetBytes("@strin3http://schemas.microsoft.com/2003/10/Serialization/�{" + json + "}");

            // Act
            var result = JobPayload.Deserialize(data);

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(jobPayload.Name, result.Name);
            Assert.AreEqual(jobPayload.Args.Length, result.Args.Length);
            Assert.AreEqual(jobPayload.Retries, result.Retries);
            Assert.AreEqual(jobPayload.Condition, result.Condition);
            Assert.AreEqual(jobPayload.Timeout, result.Timeout);
        }
    }
}
