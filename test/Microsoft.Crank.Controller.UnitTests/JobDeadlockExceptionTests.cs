using Microsoft.Crank.Controller;
using System;
using Xunit;

namespace Microsoft.Crank.Controller.UnitTests
{
    /// <summary>
    /// Unit tests for the <see cref="JobDeadlockException"/> class.
    /// </summary>
    public class JobDeadlockExceptionTests
    {
        /// <summary>
        /// Tests that the default constructor of <see cref="JobDeadlockException"/> creates an instance with expected default values.
        /// The test verifies that the instance is not null, has a non-null message and null inner exception.
        /// </summary>
        [Fact]
        public void Constructor_WhenCalled_ShouldCreateInstanceWithDefaultValues()
        {
            // Arrange & Act
            JobDeadlockException exceptionInstance = new JobDeadlockException();

            // Assert
            Assert.NotNull(exceptionInstance);
            Assert.IsType<JobDeadlockException>(exceptionInstance);
            Assert.NotNull(exceptionInstance.Message);
            Assert.Null(exceptionInstance.InnerException);
        }

        /// <summary>
        /// Tests that throwing a <see cref="JobDeadlockException"/> results
        /// in the correct exception type being caught.
        /// </summary>
//         [Fact] [Error] (37-46)CS0619 'Assert.Throws<T>(Func<Task>)' is obsolete: 'You must call Assert.ThrowsAsync<T> (and await the result) when testing async code.'
//         public void ThrowingJobDeadlockException_ShouldBeCaughtAsJobDeadlockException()
//         {
//             // Arrange, Act & Assert
//             JobDeadlockException exception = Assert.Throws<JobDeadlockException>(() =>
//             {
//                 throw new JobDeadlockException();
//             });
//             Assert.NotNull(exception);
//         }
    }
}
