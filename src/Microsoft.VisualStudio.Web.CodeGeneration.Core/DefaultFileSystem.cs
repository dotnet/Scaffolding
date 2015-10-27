// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;

namespace Microsoft.VisualStudio.Web.CodeGeneration
{
    /// <summary>
    /// Default implementation of <see cref="IFileSystem"/>
    /// using the real file sytem.
    /// </summary>
    public class DefaultFileSystem : IFileSystem
    {
        public static DefaultFileSystem Instance = new DefaultFileSystem();

        public async Task AddFileAsync(string outputPath, Stream sourceStream)
        {
            using (var writeStream = new FileStream(outputPath, FileMode.Create, FileAccess.Write))
            {
                await sourceStream.CopyToAsync(writeStream);
            }
        }

        public void CreateDirectory(string path)
        {
            Directory.CreateDirectory(path);
        }

        public bool DirectoryExists(string path)
        {
            return Directory.Exists(path);
        }

        public IEnumerable<string> EnumerateFiles(string path, string searchPattern, SearchOption searchOption)
        {
            return Directory.EnumerateFiles(path, searchPattern, searchOption);
        }

        public bool FileExists(string path)
        {
            return File.Exists(path);
        }

        public void MakeFileWritable(string path)
        {
            Debug.Assert(File.Exists(path));

            FileAttributes attributes = File.GetAttributes(path);
            if (attributes.HasFlag(FileAttributes.ReadOnly))
            {
                File.SetAttributes(path, attributes & ~FileAttributes.ReadOnly);
            }
        }

        public string ReadAllText(string path)
        {
            return File.ReadAllText(path);
        }

        public void WriteAllText(string path, string contents)
        {
            File.WriteAllText(path, contents);
        }
    }
}