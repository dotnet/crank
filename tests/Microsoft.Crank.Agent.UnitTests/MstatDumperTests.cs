using Moq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.Extensions.DependencyModel;
using Microsoft.Extensions.Logging;

namespace Microsoft.Crank.Agent.UnitTests
{
    /// <summary>
    /// Unit tests for the <see cref="MstatDumper"/> class.
    /// </summary>
    [TestClass]
    public class MstatDumperTests
    {
        private readonly Mock<ILogger> _mockLogger;

        public MstatDumperTests()
        {
            _mockLogger = new Mock<ILogger>();
        }

        /// <summary>
        /// Tests the <see cref="MstatDumper.GetInfo(string)"/> method to ensure it returns null when no .mstat files are found.
        /// </summary>
        [TestMethod]
        public void GetInfo_NoMstatFiles_ReturnsNull()
        {
            // Arrange
            var path = "testPath";
            var mockDirectory = new Mock<IDirectory>();
            mockDirectory.Setup(d => d.EnumerateFiles(path, "*.mstat", SearchOption.AllDirectories)).Returns(Enumerable.Empty<string>());

            // Act
            var result = MstatDumper.GetInfo(path);

            // Assert
            Assert.IsNull(result);
        }

        /// <summary>
        /// Tests the <see cref="MstatDumper.GetInfo(string)"/> method to ensure it returns valid results when .mstat files are found.
        /// </summary>
        [TestMethod]
        public void GetInfo_MstatFilesFound_ReturnsValidResults()
        {
            // Arrange
            var path = "testPath";
            var mstatFile = "test.mstat";
            var mockDirectory = new Mock<IDirectory>();
            mockDirectory.Setup(d => d.EnumerateFiles(path, "*.mstat", SearchOption.AllDirectories)).Returns(new[] { mstatFile });

            var mockAssemblyDefinition = new Mock<IAssemblyDefinition>();
            mockAssemblyDefinition.Setup(a => a.MainModule.LookupToken(It.IsAny<int>())).Returns(new Mock<ITypeDefinition>().Object);
            mockAssemblyDefinition.Setup(a => a.Name.Version.Major).Returns(1);

            // Act
            var result = MstatDumper.GetInfo(path);

            // Assert
            Assert.IsNotNull(result);
            Assert.IsInstanceOfType(result, typeof(DumperResults));
        }

        /// <summary>
        /// Tests the <see cref="MstatDumper.GetInfo(string)"/> method to ensure it handles exceptions correctly.
        /// </summary>
        [TestMethod]
        public void GetInfo_ExceptionThrown_ReturnsNull()
        {
            // Arrange
            var path = "testPath";
            var mstatFile = "test.mstat";
            var mockDirectory = new Mock<IDirectory>();
            mockDirectory.Setup(d => d.EnumerateFiles(path, "*.mstat", SearchOption.AllDirectories)).Returns(new[] { mstatFile });

            var mockAssemblyDefinition = new Mock<IAssemblyDefinition>();
            mockAssemblyDefinition.Setup(a => a.MainModule.LookupToken(It.IsAny<int>())).Throws(new Exception());

            // Act
            var result = MstatDumper.GetInfo(path);

            // Assert
            Assert.IsNull(result);
        }
    }
}

