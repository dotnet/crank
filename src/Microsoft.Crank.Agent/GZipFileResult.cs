// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Net.Http.Headers;

public class GZipFileResult : ActionResult
{
    public GZipFileResult(string filename)
    {
        FileName = filename;
    }

    public string FileName { get; set; }

    public override async Task ExecuteResultAsync(ActionContext context)
    {
        context.HttpContext.Response.Headers.Add(HeaderNames.Vary, HeaderNames.ContentEncoding);
        await using var stream = File.OpenRead(FileName);
        
        context.HttpContext.Response.Headers["FileLength"] = new FileInfo(FileName).Length.ToString(CultureInfo.InvariantCulture);
        
        if (context.HttpContext.Request.Headers.TryGetValue(HeaderNames.AcceptEncoding, out var acceptEncoding)
            && acceptEncoding.ToString().Contains("gzip"))
        {
            await using var gzipStream = new GZipStream(context.HttpContext.Response.Body, CompressionLevel.Fastest);

            context.HttpContext.Response.Headers[HeaderNames.ContentEncoding] = "gzip";
            await stream.CopyToAsync(gzipStream);
            await stream.FlushAsync();
            await gzipStream.FlushAsync();
        }
        else
        {
            await stream.CopyToAsync(context.HttpContext.Response.Body);
            await stream.FlushAsync();
        }
    }
}
