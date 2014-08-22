// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.IO;
using System.Linq;
using Microsoft.Framework.CodeGeneration;
using Microsoft.Framework.DependencyInjection;
using Microsoft.Framework.Runtime;

namespace Microsoft.Framework.CodeGenerators.Mvc
{
    public abstract class DependencyInstaller
    {
        protected DependencyInstaller(
            [NotNull]ILibraryManager libraryManager,
            [NotNull]IApplicationEnvironment applicationEnvironment)
        {
            LibraryManager = libraryManager;
            ApplicationEnvironment = applicationEnvironment;
        }

        public abstract void Execute();

        public virtual IEnumerable<Dependency> Dependencies
        {
            get
            {
                return Enumerable.Empty<Dependency>();
            }
        }

        public abstract string TemplateFoldersName { get; }

        protected IEnumerable<string> TemplateFolders
        {
            get
            {
                return TemplateFoldersUtilities.GetTemplateFolders(
                    containingProject: Constants.ThisAssemblyName,
                    baseFolders: new[] { TemplateFoldersName },
                    applicationBasePath: ApplicationEnvironment.ApplicationBasePath,
                    libraryManager: LibraryManager);
            }
        }

        protected IApplicationEnvironment ApplicationEnvironment { get; private set; }

        protected ILibraryManager LibraryManager { get; private set; }

        // Copies files from given source directory to destination directory recursively
        // Ignores any existing files
        protected void CopyFolderContentsRecursive(string destinationPath, string sourcePath)
        {
            DirectoryInfo sourceDir = new DirectoryInfo(sourcePath);
            Contract.Assert(sourceDir.Exists);

            // Create the destination directory if it does not exist.
            Directory.CreateDirectory(destinationPath);

            // Copy the files only if they don't exist in the destination.
            foreach (var fileInfo in sourceDir.GetFiles())
            {
                var destinationFilePath = Path.Combine(destinationPath, fileInfo.Name);
                if (!File.Exists(destinationFilePath))
                {
                    fileInfo.CopyTo(destinationFilePath);
                }
            }

            // Copy sub folder contents
            foreach (var subDirInfo in sourceDir.GetDirectories())
            {
                CopyFolderContentsRecursive(Path.Combine(destinationPath, subDirInfo.Name), subDirInfo.FullName);
            }
        }
    }
}