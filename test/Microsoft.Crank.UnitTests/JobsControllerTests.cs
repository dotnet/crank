using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Crank.Agent.Controllers;
using Microsoft.Crank.Models;
using Repository;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.Crank.UnitTests
{
    public class JobsControllerTests
    {
        private readonly ITestOutputHelper _output;

        public JobsControllerTests(ITestOutputHelper output)
        {
            _output = output;
        }

        [Theory]
        [InlineData("$&\n\r.\\execute")]
        [InlineData("../../../../../../etc/passwd")]  // Unix-style traversal
        [InlineData("..\\..\\..\\Windows\\System32\\config\\SAM")]  // Windows system files
        [InlineData("../../../../../../../var/log/syslog")]  // Absolute path traversal
        [InlineData("..\\\\..\\\\..\\\\sensitive.txt")]  // Double backslash
        [InlineData("foo/../../../etc/passwd")]  // Mixed valid and traversal
        [InlineData("/etc/passwd")]  // Absolute Unix path
        [InlineData("\\\\network\\share\\file.txt")]  // UNC path
        [InlineData("..\\..\\..\\..\\..\\..\\..\\..\\..\\..\\file.txt")]  // Excessive traversal
        [InlineData("./../../../../../../etc/shadow")]  // Dot slash mixed
        [InlineData("test/../../../../../../etc/hosts")]  // Valid then traversal
        public async Task Download_PathTraversalAttempts_ReturnsBadRequest(string path)
        {
            // Use absolute path for testing
            var tempDir = Path.GetTempPath();
            var jobDir = Path.Combine(tempDir, "crank_test_jobs", "1");
            Directory.CreateDirectory(jobDir);

            try
            {
                var jobRepo = new JobsRepository();
                jobRepo.Add(new()
                {
                    Id = 1,
                    State = JobState.Running,
                    BasePath = jobDir
                });

                var jobsController = new JobsController(jobRepo);

                var result = await jobsController.Download(1, path);
                
                // Log diagnostic information
                _output.WriteLine($"Testing path: {path}");
                _output.WriteLine($"BasePath: {jobDir}");
                _output.WriteLine($"Result type: {result.GetType().Name}");
                
                Assert.IsType<BadRequestObjectResult>(result);
            }
            finally
            {
                // Cleanup
                try
                {
                    Directory.Delete(Path.Combine(tempDir, "crank_test_jobs"), true);
                }
                catch
                {
                    // Ignore cleanup errors
                }
            }
        }

        [Fact]
        public async Task Download_DiagnosticTest_ShowsPathResolution()
        {
            var tempDir = Path.GetTempPath();
            var jobDir = Path.Combine(tempDir, "crank_test_jobs", "1");
            Directory.CreateDirectory(jobDir);

            try
            {
                var path = "..\\..\\..\\..\\..\\..\\..\\..\\..\\..\\file.txt";
                var rootPath = Directory.GetParent(jobDir).FullName;
                var fullPath = Path.GetFullPath(path, jobDir);

                _output.WriteLine($"Input path: {path}");
                _output.WriteLine($"BasePath: {jobDir}");
                _output.WriteLine($"Root path: {rootPath}");
                _output.WriteLine($"Resolved full path: {fullPath}");
                _output.WriteLine($"FullPath starts with RootPath: {fullPath.StartsWith(rootPath, System.StringComparison.OrdinalIgnoreCase)}");

                var jobRepo = new JobsRepository();
                jobRepo.Add(new()
                {
                    Id = 1,
                    State = JobState.Running,
                    BasePath = jobDir
                });

                var jobsController = new JobsController(jobRepo);
                var result = await jobsController.Download(1, path);
                
                _output.WriteLine($"Result type: {result.GetType().Name}");
                if (result is BadRequestObjectResult badRequest)
                {
                    _output.WriteLine($"Error message: {badRequest.Value}");
                }
            }
            finally
            {
                try
                {
                    Directory.Delete(Path.Combine(tempDir, "crank_test_jobs"), true);
                }
                catch
                {
                    // Ignore cleanup errors
                }
            }
        }

        [Theory]
        [InlineData("http://localhost:5000/api", "http://evil.com/steal")]  // Different domain
        [InlineData("http://localhost:5000", "http://localhost:8080/api")]  // Different port
        [InlineData("http://localhost:5000", "https://localhost:5000/api")] // Different scheme
        [InlineData("http://example.com", "http://attacker.com")]           // Completely different host
        [InlineData("http://api.example.com", "http://evil.example.com")]   // Subdomain manipulation
        [InlineData("http://localhost:5000", "//evil.com/steal")]           // Protocol-relative URL
        [InlineData("http://localhost:5000", "http://127.0.0.1:5000/api")]  // IP vs hostname
        public async Task Invoke_DifferentHostRequests_ReturnsBadRequest(string jobUrl, string path)
        {
            var jobRepo = new JobsRepository();
            jobRepo.Add(new()
            {
                Id = 1,
                State = JobState.Running,
                BasePath = Path.GetTempPath(),
                Url = jobUrl
            });

            var jobsController = new JobsController(jobRepo);
            var result = await jobsController.Invoke(1, path);

            _output.WriteLine($"Job URL: {jobUrl}");
            _output.WriteLine($"Request path: {path}");
            _output.WriteLine($"Result type: {result.GetType().Name}");

            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        }

        [Fact]
        public async Task Invoke_InvalidJobUrl_ReturnsBadRequest()
        {
            var jobRepo = new JobsRepository();
            jobRepo.Add(new()
            {
                Id = 1,
                State = JobState.Running,
                BasePath = Path.GetTempPath(),
                Url = "not-a-valid-url"  // Invalid URL
            });

            var jobsController = new JobsController(jobRepo);
            var result = await jobsController.Invoke(1, "/api/test");

            _output.WriteLine($"Result type: {result.GetType().Name}");

            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Equal("Invalid job configuration.", badRequestResult.Value);
        }

        [Fact]
        public async Task Invoke_NonExistentJob_ReturnsStatusCode500()
        {
            var jobRepo = new JobsRepository();
            var jobsController = new JobsController(jobRepo);
            
            var result = await jobsController.Invoke(999, "/api/test");

            _output.WriteLine($"Result type: {result.GetType().Name}");

            // When job is null, job.Url will throw NullReferenceException, caught by the catch block
            Assert.IsType<ObjectResult>(result);
            var objectResult = result as ObjectResult;
            Assert.Equal(500, objectResult.StatusCode);
        }

        [Theory]
        [InlineData("http://localhost:5000", "http://localhost@evil.com/steal")]  // Username in URL trick
        [InlineData("http://localhost:5000", "http://evil.com#localhost:5000")]  // Fragment manipulation
        public async Task Invoke_AdvancedSSRFAttempts_ReturnsBadRequest(string jobUrl, string path)
        {
            var jobRepo = new JobsRepository();
            jobRepo.Add(new()
            {
                Id = 1,
                State = JobState.Running,
                BasePath = Path.GetTempPath(),
                Url = jobUrl
            });

            var jobsController = new JobsController(jobRepo);
            var result = await jobsController.Invoke(1, path);

            _output.WriteLine($"Job URL: {jobUrl}");
            _output.WriteLine($"Request path: {path}");
            _output.WriteLine($"Result type: {result.GetType().Name}");

            Assert.IsType<BadRequestObjectResult>(result);
        }

        private class JobsRepository : IJobRepository
        {
            private readonly Dictionary<int, Job> _jobs = new();

            public Job Add(Job item)
            {
                _jobs.Add(item.Id, item);
                return item;
            }

            public Job Find(int id)
                => _jobs.TryGetValue(id, out var job) ? job : null;

            public IEnumerable<Job> GetAll()
                => _jobs.Values;

            public Job Remove(int id)
                => _jobs.Remove(id, out var job) ? job : null;

            public void Update(Job item)
                => _jobs[item.Id] = item;
        }
    }
}
