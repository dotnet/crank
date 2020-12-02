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
                var downloadTask = downloadStream.CopyToAsync(fileStream);

                while (!downloadTask.IsCompleted)
                {
                    // Ping server job to keep it alive while downloading the file
                    Log.Verbose($"GET {serverJobUri}/touch...");
                    await httpClient.GetAsync(serverJobUri + "/touch");

                    await Task.Delay(1000);
                }

                await downloadTask;
            }
        }

        internal static async Task DownloadFileWithProgressAsync(this HttpClient httpClient, string uri, string serverJobUri, string destinationFileName)
        {
            Log.Verbose($"GET {uri}");

            using var request = new HttpRequestMessage(HttpMethod.Get, uri);
            using var response = await httpClient.SendAsync(request);

            response.EnsureSuccessStatusCode();

            var contentLength = 0;
            if (response.Headers.TryGetValues("FileLength", out var fileLengthHeaderValues))
            {
                int.TryParse(fileLengthHeaderValues.FirstOrDefault(), out contentLength);
            }
            
            using (var downloadStream = await response.Content.ReadAsStreamAsync())
            {
                var lastMeasure = DateTime.UtcNow;
                var progressDisplayed = false;

                var progress = new Progress<long>(reportedLength => 
                {
                    var progressIsComplete = contentLength == reportedLength;
                    var updateProgress = DateTime.UtcNow - lastMeasure > TimeSpan.FromMilliseconds(1000);

                    if (progressIsComplete || updateProgress)
                    {
                        progressDisplayed = true;
                        Console.CursorLeft = 0;
                        Console.Write($"{(reportedLength / 1024):n0} KB");

                        if (contentLength != 0)
                        {
                            var progress = ((double)reportedLength / contentLength) * 100;
                            Console.Write($" / {(contentLength / 1024):n0} KB ({progress:n0}%)");
                        }

                        lastMeasure = DateTime.UtcNow;
                        Console.CursorLeft = 0;
                    }
                });

                using (var fileStream = File.Create(destinationFileName))
                {
                    var downloadTask = downloadStream.CopyToAsync(fileStream, progress);

                    while (!downloadTask.IsCompleted)
                    {
                        // Ping server job to keep it alive while downloading the file
                        Log.Verbose($"GET {serverJobUri}/touch...");
                        await httpClient.GetAsync(serverJobUri + "/touch");

                        await Task.Delay(1000);
                    }

                    await downloadTask;

                    if (progressDisplayed)
                    {
                        Console.WriteLine("");
                    }

                }
            }
        }

        internal static async Task CopyToAsync(this Stream source, Stream destination, IProgress<long> progress, CancellationToken cancellationToken = default(CancellationToken), int bufferSize = 0x1000)
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
