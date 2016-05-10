// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.VisualStudio.Web.CodeGeneration.Core;

namespace Microsoft.VisualStudio.Web.CodeGeneration
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
            string fileName,
            IEnumerable<string> searchPaths)
        {
            if (fileName == null)
            {
                throw new ArgumentNullException(nameof(fileName));
            }

            if (searchPaths == null)
            {
                throw new ArgumentNullException(nameof(searchPaths));
            }

            foreach (var searchPath in searchPaths)
            {
                if (_fileSystem.DirectoryExists(searchPath))
                {
                    var matchingFiles = _fileSystem.EnumerateFiles(searchPath,
                        searchPattern: fileName,
                        searchOption: SearchOption.AllDirectories).ToList();

                    if (matchingFiles.Count > 1)
                    {
                        throw new InvalidOperationException(string.Format(
                            MessageStrings.MultipleFilesFound,
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
                MessageStrings.FileNotFoundInFolders,
                fileName,
                string.Join(";", searchPaths)));
        }
    }
}