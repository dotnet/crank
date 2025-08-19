using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Crank.Models;
using Repository;
using Xunit;

namespace Repository.UnitTests
{
    /// <summary>
    /// Unit tests for the <see cref="InMemoryJobRepository"/> class.
    /// </summary>
    public class InMemoryJobRepositoryTests
    {
        private readonly InMemoryJobRepository _repository;

        public InMemoryJobRepositoryTests()
        {
            _repository = new InMemoryJobRepository();
        }

        /// <summary>
        /// Tests that Add method assigns an ID to a job with initial Id 0.
        /// Expected outcome: Returns a job with a non-zero Id.
        /// </summary>
        [Fact]
        public void Add_ValidJob_ReturnsJobWithAssignedId()
        {
            // Arrange
            var job = new Job { Id = 0 };

            // Act
            var addedJob = _repository.Add(job);

            // Assert
            Assert.NotNull(addedJob);
            Assert.NotEqual(0, addedJob.Id);
        }

        /// <summary>
        /// Tests that Add method throws ArgumentException when the job's Id is non-zero.
        /// Expected outcome: Throws ArgumentException.
        /// </summary>
        [Fact]
        public void Add_JobWithNonZeroId_ThrowsArgumentException()
        {
            // Arrange
            var job = new Job { Id = 100 };

            // Act & Assert
            var exception = Assert.Throws<ArgumentException>(() => _repository.Add(job));
            Assert.Equal("item.Id must be 0.", exception.Message);
        }

        /// <summary>
        /// Tests that Find method returns the job that exists in the repository.
        /// Expected outcome: Returns the previously added job.
        /// </summary>
        [Fact]
        public void Find_ExistingJob_ReturnsJob()
        {
            // Arrange
            var job = new Job { Id = 0 };
            var addedJob = _repository.Add(job);

            // Act
            var foundJob = _repository.Find(addedJob.Id);

            // Assert
            Assert.NotNull(foundJob);
            Assert.Equal(addedJob.Id, foundJob.Id);
        }

        /// <summary>
        /// Tests that Find method returns null when the job does not exist.
        /// Expected outcome: Returns null.
        /// </summary>
        [Fact]
        public void Find_NonExistingJob_ReturnsNull()
        {
            // Act
            var foundJob = _repository.Find(-1);

            // Assert
            Assert.Null(foundJob);
        }

        /// <summary>
        /// Tests that GetAll method returns all jobs that have been added.
        /// Expected outcome: Returns a collection containing all added jobs.
        /// </summary>
        [Fact]
        public void GetAll_WithMultipleJobs_ReturnsAllJobs()
        {
            // Arrange
            var job1 = new Job { Id = 0 };
            var job2 = new Job { Id = 0 };
            var addedJob1 = _repository.Add(job1);
            var addedJob2 = _repository.Add(job2);

            // Act
            IEnumerable<Job> jobs = _repository.GetAll();

            // Assert
            Assert.NotNull(jobs);
            var jobList = jobs.ToList();
            Assert.Contains(jobList, j => j.Id == addedJob1.Id);
            Assert.Contains(jobList, j => j.Id == addedJob2.Id);
            Assert.Equal(2, jobList.Count);
        }

        /// <summary>
        /// Tests that Remove method successfully removes an existing job.
        /// Expected outcome: Returns the removed job and it is no longer found.
        /// </summary>
        [Fact]
        public void Remove_ExistingJob_ReturnsRemovedJob()
        {
            // Arrange
            var job = new Job { Id = 0 };
            var addedJob = _repository.Add(job);

            // Act
            var removedJob = _repository.Remove(addedJob.Id);
            var foundAfterRemoval = _repository.Find(addedJob.Id);

            // Assert
            Assert.NotNull(removedJob);
            Assert.Equal(addedJob.Id, removedJob.Id);
            Assert.Null(foundAfterRemoval);
        }

        /// <summary>
        /// Tests that Remove method returns null when attempting to remove a non-existent job.
        /// Expected outcome: Returns null.
        /// </summary>
        [Fact]
        public void Remove_NonExistingJob_ReturnsNull()
        {
            // Act
            var removedJob = _repository.Remove(999);

            // Assert
            Assert.Null(removedJob);
        }

        /// <summary>
        /// Tests that Update method does not replace the job if the same instance is provided.
        /// Expected outcome: The job remains the same instance.
        /// </summary>
        [Fact]
        public void Update_SameInstance_NoReplacement()
        {
            // Arrange
            var job = new Job { Id = 0 };
            var addedJob = _repository.Add(job);

            // Act
            _repository.Update(addedJob);
            var foundJob = _repository.Find(addedJob.Id);

            // Assert
            Assert.Same(addedJob, foundJob);
        }

        /// <summary>
        /// Tests that Update method replaces the job when a different instance with same Id is provided.
        /// Expected outcome: The repository holds the new instance.
        /// </summary>
        [Fact]
        public void Update_DifferentInstance_ReplacesJob()
        {
            // Arrange
            var job = new Job { Id = 0 };
            var addedJob = _repository.Add(job);
            // Create a new instance with the same Id
            var updatedJob = new Job { Id = addedJob.Id };

            // Act
            _repository.Update(updatedJob);
            var foundJob = _repository.Find(addedJob.Id);

            // Assert
            Assert.NotSame(addedJob, foundJob);
            Assert.Equal(updatedJob.Id, foundJob.Id);
        }
    }
}
