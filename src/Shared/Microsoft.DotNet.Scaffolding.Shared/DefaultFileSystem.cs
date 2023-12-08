// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;

namespace Microsoft.DotNet.Scaffolding.Shared
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

        public void DeleteFile(string path)
        {
            File.Delete(path);
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

        public void RemoveDirectory(string path, bool recursive)
        {
            Directory.Delete(path, recursive);
        }

        public void WriteAllText(string path, string contents)
        {
            File.WriteAllText(path, contents);
        }
    }
}
