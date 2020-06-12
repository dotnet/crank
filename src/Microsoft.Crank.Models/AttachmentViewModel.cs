// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
