using Microsoft.AspNetCore.Http;

namespace Microsoft.Crank.Models
{
    public class AttachmentViewModel
    {
        public int Id { get; set; }
        public string DestinationFilename { get; set; }
        public IFormFile Content { get; set; }
    }
}
