// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;

namespace Microsoft.VisualStudio.Web.CodeGeneration
{
    /// <summary>
    /// Implementation of <see cref="IFileSystem"/>
    /// Records all the changes requested for the fileSystem,
    /// without persisting the changes on disk.
    /// </summary>
    public class SimulationModeFileSystem : IFileSystem
    {

        public static SimulationModeFileSystem Instance = new SimulationModeFileSystem();

        public IFileSystemChangeTracker FileSystemChangeTracker { get; private set; }
        private SimulationModeFileSystem()
        {
            FileSystemChangeTracker = new FileSystemChangeTracker();
        }

        public async Task AddFileAsync(string outputPath, Stream sourceStream)
        {
            using (var reader = new StreamReader(sourceStream))
            {
                var contents = await reader.ReadToEndAsync();
                WriteAllText(outputPath, contents);
            }
        }

        public void CreateDirectory(string path)
        {
            if (!DirectoryExists(path))
            {
                FileSystemChangeInformation addDirectoryInformation = new FileSystemChangeInformation()
                {
                    FullPath = path,
                    ChangeType = ChangeType.AddDirectory
                };

                FileSystemChangeTracker.AddChange(addDirectoryInformation);
            }
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

            // Do nothing.
            // Making file writable is always followed by an Edit to the file, which will be captured.
        }

        public string ReadAllText(string path)
        {
            return File.ReadAllText(path);
        }

        public void WriteAllText(string path, string contents)
        {
            var fileWriteInformation = new FileSystemChangeInformation()
            {
                FullPath = path,
                ChangeType = ChangeType.AddFile,
                FileContents = contents
            };

            if (FileExists(path))
            {
                fileWriteInformation.ChangeType = ChangeType.EditFile;
            }

            FileSystemChangeTracker.AddChange(fileWriteInformation);
        }
    }
}