using Microsoft.AspNetCore.Http;

namespace Microsoft.Benchmarks.Models
{
    public class AttachmentViewModel
    {
        public int Id { get; set; }
        public string DestinationFilename { get; set; }
        public IFormFile Content { get; set; }
    }
}
