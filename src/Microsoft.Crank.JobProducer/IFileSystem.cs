// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.IO;
using System.Threading.Tasks;

namespace Microsoft.Crank.JobProducer
{
    public interface IFileSystem
    {
        Task CreateDirectoryIfNotExists(string destination);
        Task<bool> FileExists(string location);
        Task<Stream> ReadFile(string source);
        Task WriteFile(Stream fileStream, string destination);
    }
}
