// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace Microsoft.VisualStudio.Web.CodeGeneration
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