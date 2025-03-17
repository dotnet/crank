using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System;
using System.Collections.Generic;
using System.Diagnostics.Tracing;
using System.Linq;
using Microsoft.Diagnostics.NETCore.Client;

namespace Microsoft.Diagnostics.Tools.Trace.UnitTests
{
    /// <summary>
    /// Unit tests for the <see cref="TraceExtensions"/> class.
    /// </summary>
    [TestClass]
    public class TraceExtensionsTests
    {
        private readonly string _validProviderString = "Microsoft-Windows-DotNETRuntime:0x1:Informational";
        private readonly string _invalidProviderString = "InvalidProvider";
        private readonly string _emptyProviderString = string.Empty;

        /// <summary>
        /// Tests the <see cref="TraceExtensions.ToCLREventPipeProviders(string)"/> method to ensure it returns the correct providers for a valid input.
        /// </summary>
        [TestMethod]
        public void ToCLREventPipeProviders_ValidInput_ReturnsCorrectProviders()
        {
            // Arrange
            string clreventslist = "gc+gchandle";

            // Act
            var result = TraceExtensions.ToCLREventPipeProviders(clreventslist);

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(1, result.Count());
            Assert.AreEqual(TraceExtensions.CLREventProviderName, result.First().Name);
        }

        /// <summary>
        /// Tests the <see cref="TraceExtensions.ToCLREventPipeProviders(string)"/> method to ensure it returns an empty collection for an empty input.
        /// </summary>
        [TestMethod]
        public void ToCLREventPipeProviders_EmptyInput_ReturnsEmptyCollection()
        {
            // Arrange
            string clreventslist = string.Empty;

            // Act
            var result = TraceExtensions.ToCLREventPipeProviders(clreventslist);

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(0, result.Count());
        }

        /// <summary>
        /// Tests the <see cref="TraceExtensions.ToProvider(string)"/> method to ensure it returns the correct provider for a valid input.
        /// </summary>
        [TestMethod]
        public void ToProvider_ValidInput_ReturnsCorrectProvider()
        {
            // Act
            var result = TraceExtensions.ToProvider(_validProviderString);

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(1, result.Count());
            Assert.AreEqual("Microsoft-Windows-DotNETRuntime", result.First().Name);
        }

        /// <summary>
        /// Tests the <see cref="TraceExtensions.ToProvider(string)"/> method to ensure it returns an empty collection for an invalid input.
        /// </summary>
        [TestMethod]
        public void ToProvider_InvalidInput_ReturnsEmptyCollection()
        {
            // Act
            var result = TraceExtensions.ToProvider(_invalidProviderString);

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(0, result.Count());
        }

        /// <summary>
        /// Tests the <see cref="TraceExtensions.ToProvider(string)"/> method to ensure it returns an empty collection for an empty input.
        /// </summary>
        [TestMethod]
        public void ToProvider_EmptyInput_ReturnsEmptyCollection()
        {
            // Act
            var result = TraceExtensions.ToProvider(_emptyProviderString);

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(0, result.Count());
        }

        /// <summary>
        /// Tests the <see cref="TraceExtensions.Merge(IEnumerable{EventPipeProvider}, IEnumerable{EventPipeProvider})"/> method to ensure it merges two collections correctly.
        /// </summary>
        [TestMethod]
        public void Merge_TwoCollections_ReturnsMergedCollection()
        {
            // Arrange
            var collection1 = new List<EventPipeProvider>
            {
                new EventPipeProvider("Provider1", EventLevel.Informational)
            };
            var collection2 = new List<EventPipeProvider>
            {
                new EventPipeProvider("Provider2", EventLevel.Verbose)
            };

            // Act
            var result = TraceExtensions.Merge(collection1, collection2);

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(2, result.Count());
            Assert.IsTrue(result.Any(p => p.Name == "Provider1"));
            Assert.IsTrue(result.Any(p => p.Name == "Provider2"));
        }
    }
}

