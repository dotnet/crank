using System;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Net.Http.Headers;
using Xunit;

namespace GZipFileResult.UnitTests
{
    /// <summary>
    /// Unit tests for the <see cref="GZipFileResult"/> class.
    /// </summary>
    public class GZipFileResultTests
    {
        /// <summary>
        /// Tests that ExecuteResultAsync compresses the file content when the Accept-Encoding header includes "gzip".
        /// The test creates a temporary file with known content, sets the Accept-Encoding header to include "gzip",
        /// invokes ExecuteResultAsync, then decompresses the response body stream and verifies that it matches the file content.
        /// It also verifies that the appropriate response headers ("Content-Encoding", "FileLength", and "Vary") are set.
        /// </summary>
//         [Fact] [Error] (35-38)CS0118 'GZipFileResult' is a namespace but is used like a type
//         public async Task ExecuteResultAsync_AcceptEncodingContainsGzip_WritesCompressedResponse()
//         {
//             // Arrange
//             string testContent = "This is some test data to compress.";
//             string tempFilePath = Path.GetTempFileName();
//             await File.WriteAllTextAsync(tempFilePath, testContent);
//             
//             try
//             {
//                 var gzipResult = new GZipFileResult(tempFilePath);
//                 var context = new ActionContext
//                 {
//                     HttpContext = new DefaultHttpContext()
//                 };
// 
//                 // Set Accept-Encoding to include "gzip"
//                 context.HttpContext.Request.Headers[HeaderNames.AcceptEncoding] = "deflate, gzip";
// 
//                 // Use a memory stream for the response body.
//                 var responseBodyStream = new MemoryStream();
//                 context.HttpContext.Response.Body = responseBodyStream;
// 
//                 // Act
//                 await gzipResult.ExecuteResultAsync(context);
// 
//                 // Assert
//                 // Check that the "Vary" header contains "Content-Encoding".
//                 Assert.True(context.HttpContext.Response.Headers.ContainsKey(HeaderNames.Vary));
//                 Assert.Contains(HeaderNames.ContentEncoding, context.HttpContext.Response.Headers[HeaderNames.Vary].ToString());
// 
//                 // Check that the "FileLength" header is set correctly.
//                 var expectedFileLength = new FileInfo(tempFilePath).Length.ToString(CultureInfo.InvariantCulture);
//                 Assert.True(context.HttpContext.Response.Headers.ContainsKey("FileLength"));
//                 Assert.Equal(expectedFileLength, context.HttpContext.Response.Headers["FileLength"].ToString());
// 
//                 // Verify that "Content-Encoding" header is set to gzip.
//                 Assert.True(context.HttpContext.Response.Headers.ContainsKey(HeaderNames.ContentEncoding));
//                 Assert.Equal("gzip", context.HttpContext.Response.Headers[HeaderNames.ContentEncoding].ToString());
// 
//                 // Read and decompress the response body.
//                 responseBodyStream.Seek(0, SeekOrigin.Begin);
//                 using var decompressionStream = new GZipStream(responseBodyStream, CompressionMode.Decompress);
//                 using var reader = new StreamReader(decompressionStream, Encoding.UTF8);
//                 string decompressedContent = await reader.ReadToEndAsync();
// 
//                 Assert.Equal(testContent, decompressedContent);
//             }
//             finally
//             {
//                 if (File.Exists(tempFilePath))
//                 {
//                     File.Delete(tempFilePath);
//                 }
//             }
//         }

        /// <summary>
        /// Tests that ExecuteResultAsync writes the file content uncompressed if the Accept-Encoding header does not include "gzip".
        /// The test creates a temporary file with known content, ensures that Accept-Encoding does not include "gzip",
        /// invokes ExecuteResultAsync, and then verifies that the response body contains the original file content.
        /// It also checks that the "Content-Encoding" header is not set.
        /// </summary>
//         [Fact] [Error] (98-38)CS0118 'GZipFileResult' is a namespace but is used like a type
//         public async Task ExecuteResultAsync_AcceptEncodingNotContainingGzip_WritesUncompressedResponse()
//         {
//             // Arrange
//             string testContent = "This is some test data without compression.";
//             string tempFilePath = Path.GetTempFileName();
//             await File.WriteAllTextAsync(tempFilePath, testContent);
//             
//             try
//             {
//                 var gzipResult = new GZipFileResult(tempFilePath);
//                 var context = new ActionContext
//                 {
//                     HttpContext = new DefaultHttpContext()
//                 };
// 
//                 // Set Accept-Encoding header to a value not containing "gzip"
//                 context.HttpContext.Request.Headers[HeaderNames.AcceptEncoding] = "deflate";
//                 
//                 // Use a memory stream for the response body.
//                 var responseBodyStream = new MemoryStream();
//                 context.HttpContext.Response.Body = responseBodyStream;
// 
//                 // Act
//                 await gzipResult.ExecuteResultAsync(context);
// 
//                 // Assert
//                 // Check that the "Vary" header contains "Content-Encoding".
//                 Assert.True(context.HttpContext.Response.Headers.ContainsKey(HeaderNames.Vary));
//                 Assert.Contains(HeaderNames.ContentEncoding, context.HttpContext.Response.Headers[HeaderNames.Vary].ToString());
// 
//                 // Check that the "FileLength" header is set correctly.
//                 var expectedFileLength = new FileInfo(tempFilePath).Length.ToString(CultureInfo.InvariantCulture);
//                 Assert.True(context.HttpContext.Response.Headers.ContainsKey("FileLength"));
//                 Assert.Equal(expectedFileLength, context.HttpContext.Response.Headers["FileLength"].ToString());
// 
//                 // Verify that the "Content-Encoding" header is not set to "gzip" (or is absent) because gzip was not requested.
//                 if (context.HttpContext.Response.Headers.ContainsKey(HeaderNames.ContentEncoding))
//                 {
//                     Assert.NotEqual("gzip", context.HttpContext.Response.Headers[HeaderNames.ContentEncoding].ToString());
//                 }
// 
//                 // Read the response body.
//                 responseBodyStream.Seek(0, SeekOrigin.Begin);
//                 using var reader = new StreamReader(responseBodyStream, Encoding.UTF8);
//                 string responseContent = await reader.ReadToEndAsync();
// 
//                 Assert.Equal(testContent, responseContent);
//             }
//             finally
//             {
//                 if (File.Exists(tempFilePath))
//                 {
//                     File.Delete(tempFilePath);
//                 }
//             }
//         }

        /// <summary>
        /// Tests that ExecuteResultAsync throws an exception when the specified file does not exist.
        /// The test provides a non-existent file path to the GZipFileResult constructor and verifies that
        /// an exception is thrown upon invoking ExecuteResultAsync.
        /// </summary>
//         [Fact] [Error] (156-34)CS0118 'GZipFileResult' is a namespace but is used like a type
//         public async Task ExecuteResultAsync_FileNotFound_ThrowsException()
//         {
//             // Arrange
//             string nonExistentFile = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString() + ".txt");
//             var gzipResult = new GZipFileResult(nonExistentFile);
//             var context = new ActionContext
//             {
//                 HttpContext = new DefaultHttpContext()
//             };
// 
//             // Use a memory stream for the response body.
//             context.HttpContext.Response.Body = new MemoryStream();
// 
//             // Act & Assert
//             await Assert.ThrowsAsync<FileNotFoundException>(() => gzipResult.ExecuteResultAsync(context));
//         }
    }
}
