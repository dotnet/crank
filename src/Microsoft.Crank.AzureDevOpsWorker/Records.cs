namespace Microsoft.Crank.AzureDevOpsWorker
{
    public class Records
    {
        public int Count { get; set; }
        public Record[] Value { get; set; }
    }

    public class Record
    {
        public string Id { get; set; }
        public string State { get; set; } // "completed", "pending"
        public string Result { get; set; } // "succeeded", "skipped", "failed", null
    }
}
