// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.VisualStudio.Web.CodeGeneration.Core.Test
{
    /// <summary>
    /// A useful helper moq for IFileSystem.
    /// Use WriteAllText, AddFolders/CreateDirectory methods to add file paths
    /// and folders paths to the file system.
    /// Maintaining the integrity of file system is the responsibility of caller.
    /// (Like creating files and folders in a proper way)
    /// </summary>
    public class MockFileSystem : IFileSystem
    {
        Dictionary<string, string> _files = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        HashSet<string> _folders = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        public void AddFolders(IEnumerable<string> folders)
        {
            foreach(var folder in folders)
            {
                CreateDirectory(folder);
            }
        }

        public void WriteAllText(string path, string contents)
        {
            _files[path] = contents;
        }

        public bool DirectoryExists(string path)
        {
            return _folders.Contains(path);
        }

        public IEnumerable<string> EnumerateFiles(string path, string searchPattern, SearchOption searchOption)
        {
            Contract.Assert(searchOption == SearchOption.AllDirectories);

            return _files
                .Where(kvp => kvp.Key.StartsWith(path) && kvp.Key.Contains(searchPattern))
                .Select(kvp => kvp.Key);
        }

        public bool FileExists(string path)
        {
            return _files.ContainsKey(path);
        }

        public void MakeFileWritable(string path)
        {
        }

        public string ReadAllText(string path)
        {
            Contract.Assert(FileExists(path));
            return _files[path];
        }

        public async Task AddFileAsync(string outputPath, Stream sourceStream)
        {
            // There could be some problems in this implementation if the
            // length is too much. Hopefully for the tests, that's not the case
            // and it's mostly ok.
            byte[] contents = new byte[sourceStream.Length];
            await sourceStream.ReadAsync(contents, 0, contents.Length);
            _files[outputPath] = Encoding.UTF8.GetString(contents);
        }

        public void CreateDirectory(string path)
        {
            _folders.Add(path);
        }
    }
}