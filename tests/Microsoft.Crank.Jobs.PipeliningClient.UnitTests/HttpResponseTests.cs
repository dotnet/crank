using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Crank.Jobs.PipeliningClient.UnitTests
{
    /// <summary>
    /// Unit tests for the <see cref="HttpResponse"/> class.
    /// </summary>
    [TestClass]
    public class HttpResponseTests
    {
        private readonly HttpResponse _httpResponse;

        public HttpResponseTests()
        {
            _httpResponse = new HttpResponse();
        }

        /// <summary>
        /// Tests the <see cref="HttpResponse.Reset"/> method to ensure it correctly resets the state of the HttpResponse object.
        /// </summary>
        [TestMethod]
        public void Reset_WhenCalled_ResetsAllPropertiesToDefaultValues()
        {
            // Arrange
            _httpResponse.State = HttpResponseState.Body;
            _httpResponse.StatusCode = 200;
            _httpResponse.ContentLength = 100;
            _httpResponse.ContentLengthRemaining = 50;
            _httpResponse.HasContentLengthHeader = true;
            _httpResponse.LastChunkRemaining = 10;

            // Act
            _httpResponse.Reset();

            // Assert
            Assert.AreEqual(HttpResponseState.StartLine, _httpResponse.State);
            Assert.AreEqual(default(int), _httpResponse.StatusCode);
            Assert.AreEqual(default(long), _httpResponse.ContentLength);
            Assert.AreEqual(default(long), _httpResponse.ContentLengthRemaining);
            Assert.AreEqual(default(bool), _httpResponse.HasContentLengthHeader);
            Assert.AreEqual(default(int), _httpResponse.LastChunkRemaining);
        }
    }
}
