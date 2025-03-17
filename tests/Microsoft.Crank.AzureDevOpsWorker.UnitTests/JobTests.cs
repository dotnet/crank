using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;

namespace Microsoft.Crank.AzureDevOpsWorker.UnitTests
{
    /// <summary>
    /// Unit tests for the <see cref="Job"/> class.
    /// </summary>
    [TestClass]
    public class JobTests
    {
        private readonly string _applicationPath = "testApp.exe";
        private readonly string _arguments = "--test";
        private readonly string _workingDirectory = "C:\\test";

        /// <summary>
        /// Tests the <see cref="Job.Start"/> method to ensure it starts the process correctly.
        /// </summary>
        [TestMethod]
        public void Start_WhenCalled_StartsProcess()
        {
            // Arrange
            var job = new Job(_applicationPath, _arguments, _workingDirectory);

            // Act
            job.Start();

            // Assert
            Assert.IsTrue(job.IsRunning);
        }

        /// <summary>
        /// Tests the <see cref="Job.Stop"/> method to ensure it stops the process correctly.
        /// </summary>
        [TestMethod]
        public void Stop_WhenCalled_StopsProcess()
        {
            // Arrange
            var job = new Job(_applicationPath, _arguments, _workingDirectory);
            job.Start();

            // Act
            job.Stop();

            // Assert
            Assert.IsFalse(job.IsRunning);
        }

        /// <summary>
        /// Tests the <see cref="Job.FlushStandardOutput"/> method to ensure it flushes the standard output correctly.
        /// </summary>
        [TestMethod]
        public void FlushStandardOutput_WhenCalled_FlushesOutput()
        {
            // Arrange
            var job = new Job(_applicationPath, _arguments, _workingDirectory);
            job.Start();
            job.Stop();

            // Act
            var output = job.FlushStandardOutput();

            // Assert
            Assert.IsNotNull(output);
        }

        /// <summary>
        /// Tests the <see cref="Job.FlushStandardError"/> method to ensure it flushes the standard error correctly.
        /// </summary>
        [TestMethod]
        public void FlushStandardError_WhenCalled_FlushesError()
        {
            // Arrange
            var job = new Job(_applicationPath, _arguments, _workingDirectory);
            job.Start();
            job.Stop();

            // Act
            var error = job.FlushStandardError();

            // Assert
            Assert.IsNotNull(error);
        }

        /// <summary>
        /// Tests the <see cref="Job.Dispose"/> method to ensure it disposes the job correctly.
        /// </summary>
        [TestMethod]
        public void Dispose_WhenCalled_DisposesJob()
        {
            // Arrange
            var job = new Job(_applicationPath, _arguments, _workingDirectory);
            job.Start();

            // Act
            job.Dispose();

            // Assert
            Assert.IsFalse(job.IsRunning);
        }

        /// <summary>
        /// Tests the <see cref="Job.WasSuccessful"/> property to ensure it returns the correct value.
        /// </summary>
        [TestMethod]
        public void WasSuccessful_WhenCalled_ReturnsCorrectValue()
        {
            // Arrange
            var job = new Job(_applicationPath, _arguments, _workingDirectory);
            job.Start();
            job.Stop();

            // Act
            var wasSuccessful = job.WasSuccessful;

            // Assert
            Assert.IsFalse(wasSuccessful);
        }

        /// <summary>
        /// Tests the <see cref="Job.IsRunning"/> property to ensure it returns the correct value.
        /// </summary>
        [TestMethod]
        public void IsRunning_WhenCalled_ReturnsCorrectValue()
        {
            // Arrange
            var job = new Job(_applicationPath, _arguments, _workingDirectory);
            job.Start();

            // Act
            var isRunning = job.IsRunning;

            // Assert
            Assert.IsTrue(isRunning);
        }
    }
}
