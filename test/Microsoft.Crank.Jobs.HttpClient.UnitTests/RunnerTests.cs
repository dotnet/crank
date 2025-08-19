using System.Net.Http;
using Jint;
using Microsoft.Crank.Jobs.HttpClientClient;
using Xunit;

namespace Microsoft.Crank.Jobs.HttpClientClient.UnitTests
{
    /// <summary>
    /// Unit tests for the <see cref="Worker"/> class.
    /// </summary>
    public class WorkerTests
    {
        private readonly Worker _worker;

        /// <summary>
        /// Initializes a new instance of the <see cref="WorkerTests"/> class with a fresh Worker instance.
        /// </summary>
        public WorkerTests()
        {
            _worker = new Worker();
        }

        /// <summary>
        /// Verifies that the default value of the Invoker property is null.
        /// </summary>
        [Fact]
        public void InvokerProperty_DefaultValue_IsNull()
        {
            // Assert
            Assert.Null(_worker.Invoker);
        }

        /// <summary>
        /// Verifies that after setting the Invoker property, the same instance is retrieved.
        /// </summary>
        [Fact]
        public void InvokerProperty_SetValue_GetReturnsSameInstance()
        {
            // Arrange
            HttpMessageInvoker expectedInvoker = new HttpClient();

            // Act
            _worker.Invoker = expectedInvoker;
            HttpMessageInvoker actualInvoker = _worker.Invoker;

            // Assert
            Assert.Equal(expectedInvoker, actualInvoker);
        }

        /// <summary>
        /// Verifies that the default value of the Handler property is null.
        /// </summary>
        [Fact]
        public void HandlerProperty_DefaultValue_IsNull()
        {
            // Assert
            Assert.Null(_worker.Handler);
        }

        /// <summary>
        /// Verifies that after setting the Handler property, the same instance is retrieved.
        /// </summary>
        [Fact]
        public void HandlerProperty_SetValue_GetReturnsSameInstance()
        {
            // Arrange
            SocketsHttpHandler expectedHandler = new SocketsHttpHandler();

            // Act
            _worker.Handler = expectedHandler;
            SocketsHttpHandler actualHandler = _worker.Handler;

            // Assert
            Assert.Equal(expectedHandler, actualHandler);
        }

        /// <summary>
        /// Verifies that the default value of the Script property is null.
        /// </summary>
        [Fact]
        public void ScriptProperty_DefaultValue_IsNull()
        {
            // Assert
            Assert.Null(_worker.Script);
        }

        /// <summary>
        /// Verifies that after setting the Script property, the same instance is retrieved.
        /// </summary>
        [Fact]
        public void ScriptProperty_SetValue_GetReturnsSameInstance()
        {
            // Arrange
            Engine expectedEngine = new Engine();

            // Act
            _worker.Script = expectedEngine;
            Engine actualEngine = _worker.Script;

            // Assert
            Assert.Equal(expectedEngine, actualEngine);
        }
    }
}
