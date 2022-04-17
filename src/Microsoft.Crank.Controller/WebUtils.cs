// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Crank.Controller
{
    internal static class WebUtils
    {
        internal static async Task<string> DownloadFileContentAsync(this HttpClient httpClient, string uri)
        {
            using (var downloadStream = await httpClient.GetStreamAsync(uri))
            {
                using (var stringReader = new StreamReader(downloadStream))
                {
                    return await stringReader.ReadToEndAsync();
                }
            }
        }

        internal static async Task DownloadFileAsync(this HttpClient httpClient, string uri, string serverJobUri, string destinationFileName)
        {
            using (var downloadStream = await httpClient.GetStreamAsync(uri))
            using (var fileStream = File.Create(destinationFileName))
            {
                await downloadStream.CopyToAsync(fileStream);
            }
        }

        internal static async Task DownloadFileWithProgressAsync(this HttpClient httpClient, string uri, string serverJobUri, string destinationFileName)
        {
            Log.Verbose($"GET {uri}");

            using var request = new HttpRequestMessage(HttpMethod.Get, uri);
            // ResponseHeadersRead is required for the content to be read later on
            using var response = await httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);

            response.EnsureSuccessStatusCode();

            var contentLength = 0;
            if (response.Headers.TryGetValues("FileLength", out var fileLengthHeaderValues))
            {
                int.TryParse(fileLengthHeaderValues.FirstOrDefault(), out contentLength);
            }
            
            using (var downloadStream = await response.Content.ReadAsStreamAsync())
            {
                var lastMeasure = DateTime.UtcNow;

                var progress = new Progress<long>(reportedLength => 
                {
                    var progressIsComplete = contentLength == reportedLength;
                    var updateProgress = DateTime.UtcNow - lastMeasure > TimeSpan.FromMilliseconds(1000);

                    if (progressIsComplete || updateProgress)
                    {
                        lock (Console.Out)
                        {
                            if (contentLength != 0)
                            {
                                var progress = ((double)reportedLength / contentLength) * 100;
                                Console.Write($"{(reportedLength / 1024):n0} KB / {(contentLength / 1024):n0} KB ({progress:n0}%)".PadRight(100));
                            }
                            else
                            {
                                Console.Write($"{(reportedLength / 1024):n0} KB".PadRight(100));
                            }

                            lastMeasure = DateTime.UtcNow;
                            
                            if (Environment.UserInteractive && !Console.IsOutputRedirected && Console.In != StreamReader.Null)
                            {
                                Console.CursorLeft = 0;
                            }
                            else
                            {
                                Console.WriteLine();
                            }
                        }
                    }
                });

                using (var fileStream = File.Create(destinationFileName))
                {
                    await downloadStream.CopyToAsync(fileStream, progress);	                    
                }
            }
        
            Console.WriteLine();
        }

        internal static async Task CopyToAsync(this Stream source, Stream destination, IProgress<long> progress, CancellationToken cancellationToken = default(CancellationToken), int bufferSize = 0x10000)
        {
            var buffer = new byte[bufferSize];
            int bytesRead;
            long totalRead = 0;

            while ((bytesRead = await source.ReadAsync(buffer, 0, buffer.Length, cancellationToken)) > 0)
            {
                await destination.WriteAsync(buffer, 0, bytesRead, cancellationToken);
                cancellationToken.ThrowIfCancellationRequested();
                totalRead += bytesRead;
                progress.Report(totalRead);
            }
        }
    }
}
