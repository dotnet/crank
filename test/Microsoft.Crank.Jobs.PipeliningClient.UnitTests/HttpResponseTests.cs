using Microsoft.Crank.Jobs.PipeliningClient;
using Xunit;

namespace Microsoft.Crank.Jobs.PipeliningClient.UnitTests
{
    /// <summary>
    /// Unit tests for the <see cref="HttpResponse"/> class.
    /// </summary>
    public class HttpResponseTests
    {
        private readonly HttpResponse _httpResponse;

        /// <summary>
        /// Initializes a new instance of the <see cref="HttpResponseTests"/> class.
        /// </summary>
        public HttpResponseTests()
        {
            _httpResponse = new HttpResponse();
        }

        /// <summary>
        /// Tests that a newly constructed <see cref="HttpResponse"/> object has the expected default property values.
        /// </summary>
        [Fact]
        public void Constructor_DefaultValues_ShouldBeCorrect()
        {
            // Arrange is done in the constructor.

            // Act
            // No action required as we only inspect the default values on a new instance.

            // Assert
            Assert.Equal(HttpResponseState.StartLine, _httpResponse.State);
            Assert.Equal(default(int), _httpResponse.StatusCode);
            Assert.Equal(default(long), _httpResponse.ContentLength);
            Assert.Equal(default(long), _httpResponse.ContentLengthRemaining);
            Assert.False(_httpResponse.HasContentLengthHeader);
            Assert.Equal(default(int), _httpResponse.LastChunkRemaining);
        }

        /// <summary>
        /// Tests that the <see cref="HttpResponse.Reset"/> method resets all properties to their default values.
        /// </summary>
        [Fact]
        public void Reset_WhenCalled_PropertiesAreResetToDefaults()
        {
            // Arrange
            _httpResponse.State = HttpResponseState.Completed;
            _httpResponse.StatusCode = 200;
            _httpResponse.ContentLength = 100L;
            _httpResponse.ContentLengthRemaining = 50L;
            _httpResponse.HasContentLengthHeader = true;
            _httpResponse.LastChunkRemaining = 20;

            // Act
            _httpResponse.Reset();

            // Assert
            Assert.Equal(HttpResponseState.StartLine, _httpResponse.State);
            Assert.Equal(default(int), _httpResponse.StatusCode);
            Assert.Equal(default(long), _httpResponse.ContentLength);
            Assert.Equal(default(long), _httpResponse.ContentLengthRemaining);
            Assert.False(_httpResponse.HasContentLengthHeader);
            Assert.Equal(default(int), _httpResponse.LastChunkRemaining);
        }
    }
}
