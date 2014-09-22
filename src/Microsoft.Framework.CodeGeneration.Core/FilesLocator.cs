// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Microsoft.Framework.CodeGeneration
{
    public class FilesLocator : IFilesLocator
    {
        private readonly IFileSystem _fileSystem;

        public FilesLocator()
            : this(new DefaultFileSystem())
        {
        }

        internal FilesLocator(IFileSystem fileSystem)
        {
            _fileSystem = fileSystem;
        }

        public string GetFilePath(
            [NotNull]string fileName,
            [NotNull]IEnumerable<string> searchPaths)
        {
            foreach (var searchPath in searchPaths)
            {
                if (_fileSystem.DirectoryExists(searchPath))
                {
                    var matchingFiles = _fileSystem.EnumerateFiles(searchPath,
                        searchPattern: fileName,
                        searchOption:  SearchOption.AllDirectories).ToList();

                    if (matchingFiles.Count > 1)
                    {
                        throw new InvalidOperationException(string.Format(
                            "Multiple files with name {0} found within {1}",
                            fileName,
                            searchPath));
                    }
                    if (matchingFiles.Count == 1)
                    {
                        return matchingFiles.Single();
                    }
                }
            }

            throw new InvalidOperationException(string.Format(
                "A file matching the name {0} was not found within any of the folders: {1}",
                fileName,
                string.Join(";", searchPaths)));
        }
    }
}