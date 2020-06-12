﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.IO;
using System.Threading.Tasks;

namespace Microsoft.Crank.JobProducer
{
    class LocalFileSystem : IFileSystem
    {
        private readonly string _basePath;

        public LocalFileSystem(string basePath)
        {
            _basePath = basePath;

            if (!Directory.Exists(basePath))
            {
                throw new ArgumentException($"No directory exists at '{basePath}'.");
            }
        }

        public Task CreateDirectoryIfNotExists(string destination)
        {
            // CreateDirectory no-ops if the subdirectory already exists.
            Directory.CreateDirectory(Path.Combine(_basePath, destination));
            return Task.CompletedTask;
        }

        public Task<bool> FileExists(string location)
        {
            return Task.FromResult(File.Exists(Path.Combine(_basePath, location)));
        }

        public Task<Stream> ReadFile(string source)
        {
            return Task.FromResult<Stream>(File.OpenRead(Path.Combine(_basePath, source)));
        }

        public async Task WriteFile(Stream fileStream, string destination)
        {
            // Write to a temp file first, so the JobConsumer doesn't see a partially written file.
            var tmpFilePath = Path.GetTempFileName();
            var tmpFile = new FileInfo(tmpFilePath);

            try
            {
                using (var tmpFileStream = File.OpenWrite(tmpFilePath))
                {
                    await fileStream.CopyToAsync(tmpFileStream);
                }

                // Moving a file is atomic.
                tmpFile.MoveTo(Path.Combine(_basePath, destination));
            }
            catch
            {
                tmpFile.Delete();
                throw;
            }
        }
    }
}
