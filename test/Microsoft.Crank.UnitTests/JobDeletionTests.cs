using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Crank.Agent;
using Microsoft.Crank.Agent.Controllers;
using Microsoft.Crank.Models;
using Repository;
using Xunit;

namespace Microsoft.Crank.UnitTests
{
    public class JobDeletionTests
    {
        [Fact]
        public void Delete_AlreadyDeleted_RepeatedRequestsPreserveTerminalState()
        {
            var repository = new InMemoryJobRepository();
            var job = repository.Add(new Job { State = JobState.Deleted });
            var controller = CreateController(repository);

            for (var request = 0; request < 2; request++)
            {
                AssertAccepted(controller, job.Id);
                Assert.Equal(JobState.Deleted, job.State);
            }

            var get = Assert.IsType<ObjectResult>(controller.GetById(job.Id));
            Assert.Same(job, get.Value);
        }

        [Fact]
        public void Delete_MissingJob_PreservesNoContentResponse()
        {
            var repository = new InMemoryJobRepository();
            var controller = CreateController(repository);

            var result = Assert.IsType<NoContentResult>(controller.Delete(999));

            Assert.Equal(StatusCodes.Status204NoContent, result.StatusCode);
            Assert.False(controller.Response.Headers.ContainsKey("Location"));
            Assert.Empty(repository.GetAll());
        }

        [Theory]
        [InlineData(JobState.New)]
        [InlineData(JobState.Initializing)]
        [InlineData(JobState.Waiting)]
        [InlineData(JobState.Starting)]
        [InlineData(JobState.Running)]
        [InlineData(JobState.Failed)]
        [InlineData(JobState.Stopping)]
        [InlineData(JobState.Stopped)]
        [InlineData(JobState.TraceCollecting)]
        [InlineData(JobState.TraceCollected)]
        [InlineData(JobState.Deleting)]
        [InlineData(JobState.NotSupported)]
        public void Delete_NonDeletedJob_RequestsCleanupAndCanBeRepeated(JobState state)
        {
            var repository = new InMemoryJobRepository();
            var job = repository.Add(new Job { State = state });
            var controller = CreateController(repository);

            AssertAccepted(controller, job.Id);
            Assert.Equal(JobState.Deleting, job.State);
            AssertAccepted(controller, job.Id);
            Assert.Equal(JobState.Deleting, job.State);
            Assert.Same(job, repository.Find(job.Id));
            Assert.Single(repository.GetAll());
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void Delete_ConcurrentTerminalPublication_PreservesDeleted(bool deleteFirst)
        {
            var entered = 0;
            var repository = new NotifyingJobRepository(() => Volatile.Write(ref entered, 1));
            var job = repository.Add(new Job { State = deleteFirst ? JobState.Stopped : JobState.Deleting });
            var controller = CreateController(repository);
            var completion = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            var timeout = TimeSpan.FromSeconds(10);
            var thread = new Thread(() =>
            {
                try
                {
                    if (deleteFirst)
                    {
                        Volatile.Write(ref entered, 1);
                        Assert.Equal(JobState.Deleting, Startup.MarkJobDeleted(job));
                    }
                    else
                    {
                        AssertAccepted(controller, job.Id);
                    }

                    completion.SetResult();
                }
                catch (Exception e)
                {
                    completion.SetException(e);
                }
            })
            {
                IsBackground = true
            };

            try
            {
                lock (job)
                {
                    thread.Start();

                    // Observe monitor contention after lookup, rather than relying on a delay.
                    Assert.True(SpinWait.SpinUntil(() => Volatile.Read(ref entered) != 0
                        && (!thread.IsAlive || (thread.ThreadState & ThreadState.WaitSleepJoin) != 0), timeout));
                    Assert.True(thread.IsAlive, "The competing transition must wait for the job monitor.");

                    if (deleteFirst)
                    {
                        AssertAccepted(controller, job.Id);
                        Assert.Equal(JobState.Deleting, job.State);
                    }
                    else
                    {
                        Assert.Equal(JobState.Deleting, Startup.MarkJobDeleted(job));
                        Assert.Equal(JobState.Deleted, job.State);
                    }
                }
            }
            finally
            {
                Assert.True(thread.Join(timeout), "The competing transition did not finish.");
            }

            completion.Task.GetAwaiter().GetResult();
            Assert.Equal(JobState.Deleted, job.State);
            Assert.Same(job, repository.Find(job.Id));
        }

        private static JobsController CreateController(IJobRepository repository)
        {
            return new JobsController(repository)
            {
                ControllerContext = new ControllerContext
                {
                    HttpContext = new DefaultHttpContext()
                }
            };
        }

        private static void AssertAccepted(JobsController controller, int id)
        {
            var result = Assert.IsType<StatusCodeResult>(controller.Delete(id));
            Assert.Equal(StatusCodes.Status202Accepted, result.StatusCode);
            Assert.Equal($"/jobs/{id}", controller.Response.Headers["Location"].ToString());
        }

        private sealed class NotifyingJobRepository : InMemoryJobRepository, IJobRepository
        {
            private readonly Action _onFind;

            public NotifyingJobRepository(Action onFind)
            {
                _onFind = onFind;
            }

            Job IJobRepository.Find(int id)
            {
                var job = Find(id);
                _onFind();
                return job;
            }
        }
    }
}
