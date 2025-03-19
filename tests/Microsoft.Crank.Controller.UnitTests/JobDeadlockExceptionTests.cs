using Microsoft.Crank.Controller;
using Xunit;

namespace Microsoft.Crank.Controller.UnitTests
{
    /// <summary>
    /// Unit tests for the <see cref="JobDeadlockException"/> class.
    /// </summary>
    public class JobDeadlockExceptionTests
    {
        /// <summary>
        /// Tests that the default constructor of JobDeadlockException creates an instance without throwing an exception.
        /// </summary>
        [Fact]
        public void Constructor_WhenCalled_DoesNotThrow()
        {
            // Act
            var exception = new JobDeadlockException();

            // Assert
            Assert.NotNull(exception);
            Assert.IsType<JobDeadlockException>(exception);
        }

        /// <summary>
        /// Tests that the default constructor of JobDeadlockException sets the expected default properties.
        /// </summary>
        [Fact]
        public void Constructor_WhenCalled_SetsDefaultProperties()
        {
            // Act
            var exception = new JobDeadlockException();

            // Assert
            // Validate that the exception message is not null or empty.
            Assert.False(string.IsNullOrEmpty(exception.Message), "Expected the exception message to be non-null and non-empty.");
            
            // Validate that the InnerException is null
            Assert.Null(exception.InnerException);
        }
    }
}
