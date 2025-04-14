using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Crank.Agent;
using Microsoft.Crank.Models;
using Microsoft.Diagnostics.NETCore.Client;
using Moq;
using Xunit;

namespace Microsoft.Crank.Agent.UnitTests
{
    /// <summary>
    /// Unit tests for the <see cref="JobContext"/> class.
    /// </summary>
    public class JobContextTests
    {
        /// <summary>
        /// Tests that when a new JobContext is instantiated, its default properties are correctly initialized.
        /// This includes verifying that SourceDirs is not null and that StartMonitorTime and NextMeasurement are set near the current UTC time.
        /// </summary>
        [Fact]
        public void Constructor_DefaultValues_ShouldInitializeProperties()
        {
            // Arrange
            DateTime beforeInitialization = DateTime.UtcNow;

            // Act
            JobContext context = new JobContext();
            DateTime afterInitialization = DateTime.UtcNow;

            // Assert
            Assert.NotNull(context.SourceDirs);
            Assert.Empty(context.SourceDirs);

            // Verify that the StartMonitorTime is set between beforeInitialization and afterInitialization
            Assert.InRange(context.StartMonitorTime, beforeInitialization, afterInitialization);
            Assert.InRange(context.NextMeasurement, beforeInitialization, afterInitialization);
        }

        /// <summary>
        /// Tests that setting and getting all properties work as expected.
        /// Sets a value for each property and then retrieves it, verifying that the get accessor returns the set value.
        /// </summary>
        [Fact]
        public void PropertyAssignment_WhenValuesSet_ShouldReturnSameValues()
        {
            // Arrange
            var expectedJob = new Job();
            var expectedProcess = new Process();
            const string expectedWorkingDirectory = @"C:\Test\WorkingDirectory";
            Timer expectedTimer = new Timer(_ => { }, null, 1000, 1000);
            const bool expectedDisposed = true;
            const string expectedBenchmarksDir = @"C:\Benchmarks";
            const string expectedTempDir = @"C:\Temp";
            const bool expectedTempDirUsesSourceKey = true;
            var expectedSourceDirs = new Dictionary<string, string> 
            {
                { "Key1", @"C:\Source1" },
                { "Key2", @"C:\Source2" }
            };
            const string expectedDockerImage = "test/image:latest";
            const string expectedDockerContainerId = "container123";
            const ulong expectedEventPipeSessionId = 123456789;
            Task expectedEventPipeTask = Task.CompletedTask;
            const bool expectedEventPipeTerminated = true;
            // For EventPipeSession, we can set null or a dummy value since no functional behavior is implemented
            EventPipeSession expectedEventPipeSession = null;
            Task expectedCountersTask = Task.CompletedTask;
            var expectedCountersCompletionSource = new TaskCompletionSource<bool>();

            JobContext context = new JobContext();

            // Act
            context.Job = expectedJob;
            context.Process = expectedProcess;
            context.WorkingDirectory = expectedWorkingDirectory;
            context.Timer = expectedTimer;
            context.Disposed = expectedDisposed;
            context.BenchmarksDir = expectedBenchmarksDir;
            context.TempDir = expectedTempDir;
            context.TempDirUsesSourceKey = expectedTempDirUsesSourceKey;
            context.SourceDirs = expectedSourceDirs;
            context.DockerImage = expectedDockerImage;
            context.DockerContainerId = expectedDockerContainerId;
            context.EventPipeSessionId = expectedEventPipeSessionId;
            context.EventPipeTask = expectedEventPipeTask;
            context.EventPipeTerminated = expectedEventPipeTerminated;
            context.EventPipeSession = expectedEventPipeSession;
            context.CountersTask = expectedCountersTask;
            context.CountersCompletionSource = expectedCountersCompletionSource;

            // Assert
            Assert.Equal(expectedJob, context.Job);
            Assert.Equal(expectedProcess, context.Process);
            Assert.Equal(expectedWorkingDirectory, context.WorkingDirectory);
            Assert.Equal(expectedTimer, context.Timer);
            Assert.Equal(expectedDisposed, context.Disposed);
            Assert.Equal(expectedBenchmarksDir, context.BenchmarksDir);
            Assert.Equal(expectedTempDir, context.TempDir);
            Assert.Equal(expectedTempDirUsesSourceKey, context.TempDirUsesSourceKey);
            Assert.Equal(expectedSourceDirs, context.SourceDirs);
            Assert.Equal(expectedDockerImage, context.DockerImage);
            Assert.Equal(expectedDockerContainerId, context.DockerContainerId);
            Assert.Equal(expectedEventPipeSessionId, context.EventPipeSessionId);
            Assert.Equal(expectedEventPipeTask, context.EventPipeTask);
            Assert.Equal(expectedEventPipeTerminated, context.EventPipeTerminated);
            Assert.Equal(expectedEventPipeSession, context.EventPipeSession);
            Assert.Equal(expectedCountersTask, context.CountersTask);
            Assert.Equal(expectedCountersCompletionSource, context.CountersCompletionSource);
        }

        /// <summary>
        /// Tests updating the dictionary property SourceDirs by adding and retrieving key-value pairs.
        /// This ensures that the dictionary behaves as expected.
        /// </summary>
        [Fact]
        public void SourceDirs_Modification_ShouldReflectChanges()
        {
            // Arrange
            JobContext context = new JobContext();
            var key = "ProjectPath";
            var value = @"C:\Projects\MyProject";

            // Act
            context.SourceDirs[key] = value;

            // Assert
            Assert.True(context.SourceDirs.ContainsKey(key));
            Assert.Equal(value, context.SourceDirs[key]);
        }
    }
}
