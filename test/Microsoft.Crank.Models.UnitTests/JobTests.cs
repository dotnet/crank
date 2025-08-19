using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.Crank.Models;
using Xunit;

namespace Microsoft.Crank.Models.UnitTests
{
    /// <summary>
    /// Unit tests for the <see cref="Job"/> class.
    /// </summary>
    public class JobTests
    {
        /// <summary>
        /// Tests the IsDocker method returning true when DockerFile is provided.
        /// </summary>
        [Fact]
        public void IsDocker_WithDockerFileSet_ReturnsTrue()
        {
            // Arrange
            var job = new Job
            {
                DockerFile = "Dockerfile"
            };

            // Act
            bool result = job.IsDocker();

            // Assert
            Assert.True(result);
        }

        /// <summary>
        /// Tests the IsDocker method returning true when DockerImageName is provided.
        /// </summary>
        [Fact]
        public void IsDocker_WithDockerImageNameSet_ReturnsTrue()
        {
            // Arrange
            var job = new Job
            {
                DockerImageName = "myimage"
            };

            // Act
            bool result = job.IsDocker();

            // Assert
            Assert.True(result);
        }

        /// <summary>
        /// Tests the IsDocker method returning true when DockerPull is provided.
        /// </summary>
        [Fact]
        public void IsDocker_WithDockerPullSet_ReturnsTrue()
        {
            // Arrange
            var job = new Job
            {
                DockerPull = "ubuntu:latest"
            };

            // Act
            bool result = job.IsDocker();

            // Assert
            Assert.True(result);
        }

        /// <summary>
        /// Tests the IsDocker method returning false when no docker properties are set.
        /// </summary>
        [Fact]
        public void IsDocker_WithNoDockerPropertiesSet_ReturnsFalse()
        {
            // Arrange
            var job = new Job
            {
                DockerFile = null,
                DockerImageName = string.Empty,
                DockerPull = string.Empty
            };

            // Act
            bool result = job.IsDocker();

            // Assert
            Assert.False(result);
        }

        /// <summary>
        /// Tests GetNormalizedImageName method returns lowercased DockerPull when provided.
        /// </summary>
        [Fact]
        public void GetNormalizedImageName_WithDockerPullSet_ReturnsLowerCaseDockerPull()
        {
            // Arrange
            var job = new Job
            {
                DockerPull = "UBUNTU:LATEST",
                DockerLoad = string.Empty,
                DockerImageName = string.Empty,
                DockerFile = string.Empty
            };

            // Act
            string result = job.GetNormalizedImageName();

            // Assert
            Assert.Equal("ubuntu:latest", result);
        }

        /// <summary>
        /// Tests GetNormalizedImageName method returns DockerImageName when DockerLoad is provided.
        /// </summary>
        [Fact]
        public void GetNormalizedImageName_WithDockerLoadSet_ReturnsDockerImageName()
        {
            // Arrange
            var job = new Job
            {
                DockerPull = string.Empty,
                DockerLoad = "someLoadOption",
                DockerImageName = "CustomImage",
                DockerFile = string.Empty
            };

            // Act
            string result = job.GetNormalizedImageName();

            // Assert
            Assert.Equal("CustomImage", result);
        }

        /// <summary>
        /// Tests GetNormalizedImageName method preserves DockerImageName if it already starts with "benchmarks_".
        /// </summary>
        [Fact]
        public void GetNormalizedImageName_WithPrefixedDockerImageName_ReturnsSameValue()
        {
            // Arrange
            var job = new Job
            {
                DockerPull = string.Empty,
                DockerLoad = string.Empty,
                DockerImageName = "benchmarks_customimage",
                DockerFile = string.Empty
            };

            // Act
            string result = job.GetNormalizedImageName();

            // Assert
            Assert.Equal("benchmarks_customimage", result);
        }

        /// <summary>
        /// Tests GetNormalizedImageName method prefixes DockerImageName with "benchmarks_" and lowercases the result when not already prefixed.
        /// </summary>
        [Fact]
        public void GetNormalizedImageName_WithDockerImageNameNotPrefixed_ReturnsPrefixedLowerCaseValue()
        {
            // Arrange
            var job = new Job
            {
                DockerPull = string.Empty,
                DockerLoad = string.Empty,
                DockerImageName = "CustomImage",
                DockerFile = string.Empty
            };

            // Act
            string result = job.GetNormalizedImageName();

            // Assert
            Assert.Equal("benchmarks_customimage", result);
        }

        /// <summary>
        /// Tests GetNormalizedImageName method falls back to DockerFile when no other docker properties are provided.
        /// </summary>
        [Fact]
        public void GetNormalizedImageName_WithDockerFileSet_ReturnsPrefixedFileName()
        {
            // Arrange
            var fileName = "myDockerFile.dkr";
            var job = new Job
            {
                DockerPull = string.Empty,
                DockerLoad = string.Empty,
                DockerImageName = string.Empty,
                DockerFile = fileName
            };

            // Act
            string result = job.GetNormalizedImageName();
            string expectedFileNameWithoutExtension = Path.GetFileNameWithoutExtension(fileName);
            string expected = $"benchmarks_{expectedFileNameWithoutExtension}".ToLowerInvariant();

            // Assert
            Assert.Equal(expected, result);
        }

        /// <summary>
        /// Tests CalculateCpuList returns an empty list when CpuSet is null or whitespace.
        /// </summary>
        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public void CalculateCpuList_WithNullOrWhitespace_ReturnsEmptyList(string cpuSet)
        {
            // Arrange
            var job = new Job
            {
                CpuSet = cpuSet
            };

            // Act
            List<int> result = job.CalculateCpuList();

            // Assert
            Assert.Empty(result);
        }

        /// <summary>
        /// Tests CalculateCpuList returns a single number when CpuSet is a single CPU value.
        /// </summary>
        [Fact]
        public void CalculateCpuList_WithSingleValue_ReturnsSingleNumber()
        {
            // Arrange
            var job = new Job
            {
                CpuSet = "3"
            };

            // Act
            List<int> result = job.CalculateCpuList();

            // Assert
            Assert.Single(result);
            Assert.Equal(3, result.First());
        }

        /// <summary>
        /// Tests CalculateCpuList returns a range of numbers when CpuSet is a range.
        /// </summary>
        [Fact]
        public void CalculateCpuList_WithRangeValue_ReturnsRangeOfNumbers()
        {
            // Arrange
            var job = new Job
            {
                CpuSet = "2-5"
            };

            // Act
            List<int> result = job.CalculateCpuList();

            // Assert
            var expected = new List<int> { 2, 3, 4, 5 };
            Assert.Equal(expected, result);
        }

        /// <summary>
        /// Tests CalculateCpuList returns correct list when CpuSet contains both single values and ranges.
        /// </summary>
        [Fact]
        public void CalculateCpuList_WithMixedValues_ReturnsCombinedList()
        {
            // Arrange
            var job = new Job
            {
                CpuSet = "1,3-5,7"
            };

            // Act
            List<int> result = job.CalculateCpuList();

            // Assert
            var expected = new List<int> { 1, 3, 4, 5, 7 };
            Assert.Equal(expected, result);
        }

        /// <summary>
        /// Tests GetBuildKeyData method to ensure it correctly reflects the properties of the Job.
        /// </summary>
        [Fact]
        public void GetBuildKeyData_WithPropertiesSet_MapsJobPropertiesToBuildKeyData()
        {
            // Arrange
            var job = new Job
            {
                Project = "TestProject",
                RuntimeVersion = "1.0.0",
                DesktopVersion = "2.0.0",
                AspNetCoreVersion = "3.0.0",
                SdkVersion = "4.0.0",
                Framework = "net5.0",
                Channel = "stable",
                PatchReferences = true,
                NoGlobalJson = true,
                UseRuntimeStore = true,
                BuildKey = "dummy",
                SelfContained = true,
                Executable = "test.exe",
                Collect = true,
                UseMonoRuntime = "mono",
                DockerLoad = "dockerLoad",
                DockerPull = "dockerPull",
                DockerFile = "Dockerfile.txt",
                DockerImageName = "mydockerimage",
                DockerContextDirectory = "contextDir",
            };
            // Set collections
            job.BuildAttachments.Add(new Attachment());
            job.Options.DownloadFilesOutput = "output"; // Just to simulate some non-related field.

            // Do not add sources so that BuildKeyData.Sources will be empty
            // Act
            BuildKeyData buildKeyData = job.GetBuildKeyData();

            // Assert
            Assert.NotNull(buildKeyData);
            Assert.Empty(buildKeyData.Sources);
            Assert.Equal(job.Project, buildKeyData.Project);
            Assert.Equal(job.RuntimeVersion, buildKeyData.RuntimeVersion);
            Assert.Equal(job.DesktopVersion, buildKeyData.DesktopVersion);
            Assert.Equal(job.AspNetCoreVersion, buildKeyData.AspNetCoreVersion);
            Assert.Equal(job.SdkVersion, buildKeyData.SdkVersion);
            Assert.Equal(job.Framework, buildKeyData.Framework);
            Assert.Equal(job.Channel, buildKeyData.Channel);
            Assert.Equal(job.PatchReferences, buildKeyData.PatchReferences);
            Assert.Equal(job.PackageReferences, buildKeyData.PackageReferences);
            Assert.Equal(job.NoGlobalJson, buildKeyData.NoGlobalJson);
            Assert.Equal(job.UseRuntimeStore, buildKeyData.UseRuntimeStore);
            Assert.Equal(job.BuildArguments, buildKeyData.BuildArguments);
            Assert.Equal(job.SelfContained, buildKeyData.SelfContained);
            Assert.Equal(job.Executable, buildKeyData.Executable);
            Assert.Equal(job.Collect, buildKeyData.Collect);
            Assert.Equal(job.UseMonoRuntime, buildKeyData.UseMonoRuntime);
            // Options collections are copied
            Assert.Equal(job.Options.BuildFiles, buildKeyData.BuildFiles);
            Assert.Equal(job.Options.BuildArchives, buildKeyData.BuildArchives);
            Assert.Equal(job.Options.OutputFiles, buildKeyData.OutputFiles);
            Assert.Equal(job.Options.OutputArchives, buildKeyData.OutputArchives);
            Assert.Equal(job.CollectDependencies, buildKeyData.CollectDependencies);
            Assert.Equal(job.DockerLoad, buildKeyData.DockerLoad);
            Assert.Equal(job.DockerPull, buildKeyData.DockerPull);
            Assert.Equal(job.DockerFile, buildKeyData.DockerFile);
            Assert.Equal(job.DockerImageName, buildKeyData.DockerImageName);
            Assert.Equal(job.DockerContextDirectory, buildKeyData.DockerContextDirectory);
        }
    }
}
