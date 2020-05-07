namespace Microsoft.Crank.Controller
{
    public class ExecutionResult
    {
        public int ReturnCode { get; set; }

        public JobResults JobResults { get; set; } = new JobResults();
    }
}
