using System.Collections.Generic;

namespace Microsoft.Benchmarks.AzDoConsumer
{
    public class JobDefinitions
    {
        public Dictionary<string, JobDefinition> Jobs { get; set; } = new Dictionary<string, JobDefinition>();
    }

    public class JobDefinition
    {
        public string Executable { get; set; }
        public string[] Arguments { get; set; }
        public Dictionary<string, string> Bindings { get; set; } = new Dictionary<string, string>();
    }
}
