using Microsoft.Crank.Models;
using Moq;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Xunit;

namespace Microsoft.Crank.Controller.UnitTests
{
    /// <summary>
    /// Unit tests for the <see cref="ResultComparer"/> class.
    /// </summary>
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
        [Fact]
        public void Compare_FileDoesNotExist_ReturnsMinusOne()
        {
            // Arrange
            var filenames = new List<string> { "file1.json", "file2.json" };
            _fileSystemMock.Setup(fs => fs.Exists(It.IsAny<string>())).Returns(false);

            // Act
            var result = ResultComparer.Compare(filenames);

            // Assert
            Assert.Equal(-1, result);
            _logMock.Verify(log => log.Write(It.IsAny<string>(), true), Times.Once);
        }

        /// <summary>
        /// Tests the <see cref="ResultComparer.Compare(IEnumerable{string}, JobResults, Benchmark[], string)"/> method to ensure it returns 0 when all files exist.
        /// </summary>
        [Fact]
        public void Compare_AllFilesExist_ReturnsZero()
        {
            // Arrange
            var filenames = new List<string> { "file1.json", "file2.json" };
            _fileSystemMock.Setup(fs => fs.Exists(It.IsAny<string>())).Returns(true);
            _fileSystemMock.Setup(fs => fs.ReadAllText(It.IsAny<string>())).Returns("{}");

            // Act
            var result = ResultComparer.Compare(filenames);

            // Assert
            Assert.Equal(0, result);
        }

        /// <summary>
        /// Tests the <see cref="ResultComparer.Compare(IEnumerable{string}, JobResults, Benchmark[], string)"/> method to ensure it handles null jobResults and benchmarks correctly.
        /// </summary>
        [Fact]
        public void Compare_NullJobResultsAndBenchmarks_ReturnsZero()
        {
            // Arrange
            var filenames = new List<string> { "file1.json", "file2.json" };
            _fileSystemMock.Setup(fs => fs.Exists(It.IsAny<string>())).Returns(true);
            _fileSystemMock.Setup(fs => fs.ReadAllText(It.IsAny<string>())).Returns("{}");

            // Act
            var result = ResultComparer.Compare(filenames, null, null, "jobName");

            // Assert
            Assert.Equal(0, result);
        }

        /// <summary>
        /// Tests the <see cref="ResultComparer.Compare(IEnumerable{string}, JobResults, Benchmark[], string)"/> method to ensure it handles non-null jobResults and benchmarks correctly.
        /// </summary>
        [Fact]
        public void Compare_NonNullJobResultsAndBenchmarks_ReturnsZero()
        {
            // Arrange
            var filenames = new List<string> { "file1.json", "file2.json" };
            var jobResults = new JobResults();
            var benchmarks = new Benchmark[0];
            _fileSystemMock.Setup(fs => fs.Exists(It.IsAny<string>())).Returns(true);
            _fileSystemMock.Setup(fs => fs.ReadAllText(It.IsAny<string>())).Returns("{}");

            // Act
            var result = ResultComparer.Compare(filenames, jobResults, benchmarks, "jobName");

            // Assert
            Assert.Equal(0, result);
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
