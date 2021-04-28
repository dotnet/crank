namespace Microsoft.Crank.Models
{
    public class Dependency
    {
        public string[] AssemblyNames { get; set; }
        public string RepositoryUrl { get; set; }
        public string Version { get; set; }
        public string CommitHash { get; set; }
    }
}
