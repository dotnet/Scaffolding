// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.IO;
using System.Linq;

namespace Microsoft.Framework.CodeGeneration.Core.Test
{
    /// <summary>
    /// A useful helper moq for IFileSystem.
    /// Use AddFile, AddFolder method to add file paths and folders paths to the file system.
    /// Maintaining the integrity of file system is the responsibility of caller. (Like creating
    /// files and folders in a proper way)
    /// </summary>
    public class MockFileSystem : IFileSystem
    {
        Dictionary<string, string> _files = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        HashSet<string> _folders = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        public void AddFolder(string path)
        {
            _folders.Add(path);
        }

        public void AddFolders(IEnumerable<string> folders)
        {
            foreach(var folder in folders)
            {
                _folders.Add(folder);
            }
        }

        public void AddFile(string path, string contents)
        {
            _files.Add(path, contents);
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
    }
}