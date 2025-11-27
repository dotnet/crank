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
        [InlineData("$&\n\r.\\execute")] // Dangerous characters
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
