using Microsoft.Crank.Models;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Microsoft.Crank.Controller.UnitTests
{
    /// <summary>
    /// Unit tests for the <see cref="ResultComparer"/> class.
    /// </summary>
    [TestClass]
    public class ResultComparerTests
    {
        private readonly Mock<IFileSystem> _fileSystemMock;
        private readonly Mock<ILog> _logMock;

        public ResultComparerTests()
        {
            _fileSystemMock = new Mock<IFileSystem>();
            _logMock = new Mock<ILog>();
        }

        /// <summary>
        /// Tests the <see cref="ResultComparer.Compare(IEnumerable{string}, JobResults, Benchmark[], string)"/> method to ensure it returns -1 when a file does not exist.
        /// </summary>
        [TestMethod]
        public void Compare_FileDoesNotExist_ReturnsMinusOne()
        {
            // Arrange
            var filenames = new List<string> { "file1.json", "file2.json" };
            _fileSystemMock.Setup(fs => fs.Exists(It.IsAny<string>())).Returns(false);

            // Act
            var result = ResultComparer.Compare(filenames);

            // Assert
            Assert.AreEqual(-1, result);
            _logMock.Verify(log => log.Write(It.IsAny<string>(), true), Times.Once);
        }

        /// <summary>
        /// Tests the <see cref="ResultComparer.Compare(IEnumerable{string}, JobResults, Benchmark[], string)"/> method to ensure it returns 0 when all files exist and are valid.
        /// </summary>
        [TestMethod]
        public void Compare_AllFilesExistAndValid_ReturnsZero()
        {
            // Arrange
            var filenames = new List<string> { "file1.json", "file2.json" };
            var executionResult = new ExecutionResult
            {
                JobResults = new JobResults(),
                Benchmarks = new Benchmark[0]
            };
            var json = JsonConvert.SerializeObject(executionResult);
            _fileSystemMock.Setup(fs => fs.Exists(It.IsAny<string>())).Returns(true);
            _fileSystemMock.Setup(fs => fs.ReadAllText(It.IsAny<string>())).Returns(json);

            // Act
            var result = ResultComparer.Compare(filenames);

            // Assert
            Assert.AreEqual(0, result);
        }

        /// <summary>
        /// Tests the <see cref="ResultComparer.Compare(IEnumerable{string}, JobResults, Benchmark[], string)"/> method to ensure it handles additional job results and benchmarks correctly.
        /// </summary>
        [TestMethod]
        public void Compare_WithAdditionalJobResultsAndBenchmarks_ReturnsZero()
        {
            // Arrange
            var filenames = new List<string> { "file1.json", "file2.json" };
            var executionResult = new ExecutionResult
            {
                JobResults = new JobResults(),
                Benchmarks = new Benchmark[0]
            };
            var json = JsonConvert.SerializeObject(executionResult);
            _fileSystemMock.Setup(fs => fs.Exists(It.IsAny<string>())).Returns(true);
            _fileSystemMock.Setup(fs => fs.ReadAllText(It.IsAny<string>())).Returns(json);

            var additionalJobResults = new JobResults();
            var additionalBenchmarks = new Benchmark[0];
            var jobName = "additionalJob";

            // Act
            var result = ResultComparer.Compare(filenames, additionalJobResults, additionalBenchmarks, jobName);

            // Assert
            Assert.AreEqual(0, result);
        }
    }

    public interface IFileSystem
    {
        bool Exists(string path);
        string ReadAllText(string path);
    }

    public interface ILog
    {
        void Write(string message, bool notime);
    }
}
