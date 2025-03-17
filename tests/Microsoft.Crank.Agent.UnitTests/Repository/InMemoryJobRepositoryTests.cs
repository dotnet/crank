using Moq;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using Repository;
using Microsoft.Crank.Models;

namespace Repository.UnitTests
{
    /// <summary>
    /// Unit tests for the <see cref="InMemoryJobRepository"/> class.
    /// </summary>
    [TestClass]
    public class InMemoryJobRepositoryTests
    {
        private readonly InMemoryJobRepository _repository;

        public InMemoryJobRepositoryTests()
        {
            _repository = new InMemoryJobRepository();
        }

        /// <summary>
        /// Tests the <see cref="InMemoryJobRepository.Add(Job)"/> method to ensure it correctly adds a job with an ID of 0.
        /// </summary>
        [TestMethod]
        public void Add_ValidJob_ReturnsJobWithId()
        {
            // Arrange
            var job = new Job { Id = 0 };

            // Act
            var result = _repository.Add(job);

            // Assert
            Assert.AreEqual(1, result.Id);
            Assert.AreEqual(job, result);
        }

        /// <summary>
        /// Tests the <see cref="InMemoryJobRepository.Add(Job)"/> method to ensure it throws an <see cref="ArgumentException"/> when the job ID is not 0.
        /// </summary>
        [TestMethod]
        public void Add_JobWithNonZeroId_ThrowsArgumentException()
        {
            // Arrange
            var job = new Job { Id = 1 };

            // Act & Assert
            Assert.ThrowsException<ArgumentException>(() => _repository.Add(job));
        }

        /// <summary>
        /// Tests the <see cref="InMemoryJobRepository.Find(int)"/> method to ensure it correctly finds a job by ID.
        /// </summary>
        [TestMethod]
        public void Find_ExistingJobId_ReturnsJob()
        {
            // Arrange
            var job = new Job { Id = 0 };
            var addedJob = _repository.Add(job);

            // Act
            var result = _repository.Find(addedJob.Id);

            // Assert
            Assert.AreEqual(addedJob, result);
        }

        /// <summary>
        /// Tests the <see cref="InMemoryJobRepository.Find(int)"/> method to ensure it returns null for a non-existing job ID.
        /// </summary>
        [TestMethod]
        public void Find_NonExistingJobId_ReturnsNull()
        {
            // Act
            var result = _repository.Find(999);

            // Assert
            Assert.IsNull(result);
        }

        /// <summary>
        /// Tests the <see cref="InMemoryJobRepository.GetAll()"/> method to ensure it returns all jobs.
        /// </summary>
        [TestMethod]
        public void GetAll_WhenCalled_ReturnsAllJobs()
        {
            // Arrange
            var job1 = new Job { Id = 0 };
            var job2 = new Job { Id = 0 };
            _repository.Add(job1);
            _repository.Add(job2);

            // Act
            var result = _repository.GetAll();

            // Assert
            CollectionAssert.Contains(result, job1);
            CollectionAssert.Contains(result, job2);
        }

        /// <summary>
        /// Tests the <see cref="InMemoryJobRepository.Remove(int)"/> method to ensure it correctly removes a job by ID.
        /// </summary>
        [TestMethod]
        public void Remove_ExistingJobId_ReturnsRemovedJob()
        {
            // Arrange
            var job = new Job { Id = 0 };
            var addedJob = _repository.Add(job);

            // Act
            var result = _repository.Remove(addedJob.Id);

            // Assert
            Assert.AreEqual(addedJob, result);
        }

        /// <summary>
        /// Tests the <see cref="InMemoryJobRepository.Remove(int)"/> method to ensure it returns null for a non-existing job ID.
        /// </summary>
        [TestMethod]
        public void Remove_NonExistingJobId_ReturnsNull()
        {
            // Act
            var result = _repository.Remove(999);

            // Assert
            Assert.IsNull(result);
        }

        /// <summary>
        /// Tests the <see cref="InMemoryJobRepository.Update(Job)"/> method to ensure it correctly updates an existing job.
        /// </summary>
        [TestMethod]
        public void Update_ExistingJob_UpdatesJob()
        {
            // Arrange
            var job = new Job { Id = 0 };
            var addedJob = _repository.Add(job);
            var updatedJob = new Job { Id = addedJob.Id, Name = "Updated Job" };

            // Act
            _repository.Update(updatedJob);
            var result = _repository.Find(updatedJob.Id);

            // Assert
            Assert.AreEqual(updatedJob, result);
        }
    }
}

