// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace Microsoft.DotNet.Scaffolding.Shared
{
    /// <summary>
    /// An abstraction over common file/disk utilities.
    /// Intended for mocking the disk operations in unit tests
    /// by providing an alternate mock implemention.
    /// </summary>
    public interface IFileSystem
    {
        bool FileExists(string path);

        bool DirectoryExists(string path);

        IEnumerable<string> EnumerateFiles(string path, string searchPattern, SearchOption searchOption);

        void MakeFileWritable(string path);

        string ReadAllText(string path);

        void WriteAllText(string path, string contents);

        Task AddFileAsync(string outputPath, Stream sourceStream);

        void CreateDirectory(string path);

        void DeleteFile(string path);

        void RemoveDirectory(string path, bool removeIfNotEmpty);
    }
}
