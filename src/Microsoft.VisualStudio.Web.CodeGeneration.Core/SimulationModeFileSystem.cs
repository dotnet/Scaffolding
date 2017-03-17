// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Web.CodeGeneration.Contracts.FileSystemChange;
using Microsoft.VisualStudio.Web.CodeGeneration.Core;

namespace Microsoft.VisualStudio.Web.CodeGeneration
{
    /// <summary>
    /// Implementation of <see cref="IFileSystem"/>
    /// Records all the changes requested for the fileSystem,
    /// without persisting the changes on disk.
    /// </summary>
    public class SimulationModeFileSystem : IFileSystem
    {

#if NET451
        private static readonly StringComparison PathComparisonType = StringComparison.OrdinalIgnoreCase;
#else
        private static readonly StringComparison PathComparisonType = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) || RuntimeInformation.IsOSPlatform(OSPlatform.OSX)
            ? StringComparison.OrdinalIgnoreCase
            : StringComparison.Ordinal;
#endif

        public static SimulationModeFileSystem Instance = new SimulationModeFileSystem();

        private IFileSystemChangeTracker FileSystemChangeTracker { get; set; }

        internal SimulationModeFileSystem()
        {
            FileSystemChangeTracker = new FileSystemChangeTracker();
        }

        public IEnumerable<FileSystemChangeInformation> FileSystemChanges
        {
            get
            {
                return FileSystemChangeTracker.Changes;
            }
        }

        #region File operations
        public async Task AddFileAsync(string outputPath, Stream sourceStream)
        {
            using (var reader = new StreamReader(sourceStream))
            {
                var contents = await reader.ReadToEndAsync();
                WriteAllText(outputPath, contents);
            }
        }

        public bool FileExists(string path)
        {
            return File.Exists(path)
                && (!FileSystemChanges.Any(
                    f => f.FullPath.Equals(path, PathComparisonType)
                        && f.FileSystemChangeType == FileSystemChangeType.DeleteFile));
        }

        public void MakeFileWritable(string path)
        {
            Debug.Assert(File.Exists(path));

            // Do nothing.
            // Making file writable is always followed by an Edit to the file, which will be captured.
        }

        public string ReadAllText(string path)
        {
            var text = FileSystemChanges
                .FirstOrDefault(f => f.FullPath.Equals(path, PathComparisonType))
                ?.FileContents;

            return text ?? File.ReadAllText(path);
        }

        public void WriteAllText(string path, string contents)
        {
            if (!DirectoryExists(Path.GetDirectoryName(path)))
            {
                throw new IOException(string.Format(MessageStrings.PathNotFound, path));
            }

            var fileWriteInformation = new FileSystemChangeInformation()
            {
                FullPath = path,
                FileSystemChangeType = FileSystemChangeType.AddFile,
                FileContents = contents
            };

            if (FileExists(path))
            {
                fileWriteInformation.FileSystemChangeType = FileSystemChangeType.EditFile;
            }

            FileSystemChangeTracker.AddChange(fileWriteInformation);
        }

        public void DeleteFile(string path)
        {
            if (!FileExists(path))
            {
                throw new IOException(string.Format(MessageStrings.PathNotFound, path));
            }

            var change = FileSystemChanges.FirstOrDefault(f => f.FullPath.Equals(path, PathComparisonType)
                && f.FileSystemChangeType == FileSystemChangeType.AddFile);

            if (change != null)
            {
                FileSystemChangeTracker.RemoveChange(change);
            }
            else
            {
                FileSystemChangeTracker.AddChange(new FileSystemChangeInformation()
                {
                    FullPath = path,
                    FileSystemChangeType = FileSystemChangeType.DeleteFile
                });
            }

        }
        #endregion

        #region Directory Operations
        public void CreateDirectory(string path)
        {
            if (!DirectoryExists(path))
            {
                var change = FileSystemChanges.FirstOrDefault(f => f.FullPath.Equals(path, PathComparisonType)
                    && f.FileSystemChangeType == FileSystemChangeType.RemoveDirectory);

                if (change != null)
                {
                    // If the directory was deleted, just remove the change.
                    // The directory could have been deleted to remove its children then added back.
                    FileSystemChangeTracker.RemoveChange(change);
                }
                else
                {
                    FileSystemChangeInformation addDirectoryInformation = new FileSystemChangeInformation()
                    {
                        FullPath = path,
                        FileSystemChangeType = FileSystemChangeType.AddDirectory
                    };

                    FileSystemChangeTracker.AddChange(addDirectoryInformation);
                }

                var deletedParents = FileSystemChanges.Where(f => path.StartsWith(f.FullPath) && f.FileSystemChangeType == FileSystemChangeType.RemoveDirectory);
                foreach(var deletedParent in deletedParents)
                {
                    FileSystemChangeTracker.RemoveChange(deletedParent);
                }
            }
        }

        public bool DirectoryExists(string path)
        {
            var fileSystemChange = FileSystemChanges.FirstOrDefault(f => f.FullPath.Equals(path, PathComparisonType));

            if (fileSystemChange == null)
            {
                return Directory.Exists(path);
            }

            return (Directory.Exists(path) && !(fileSystemChange.FileSystemChangeType == FileSystemChangeType.RemoveDirectory))
                || (fileSystemChange.FileSystemChangeType == FileSystemChangeType.AddDirectory);
                
        }

        public void RemoveDirectory(string path, bool recursive)
        {
            if (!DirectoryExists(path))
            {
                throw new IOException(string.Format(MessageStrings.PathNotFound, path));
            }

            var change = FileSystemChanges.FirstOrDefault(f => f.FullPath.Equals(path, PathComparisonType));

            var subDirectoryChanges = FileSystemChanges.Where(f => f.FullPath.StartsWith(path, PathComparisonType));
            if (!recursive) {
                if (change != null && change.FileSystemChangeType == FileSystemChangeType.AddDirectory)
                {
                    if (subDirectoryChanges.Any())
                    {
                        throw new IOException(string.Format(MessageStrings.DirectoryNotEmpty, path));
                    }

                    FileSystemChangeTracker.RemoveChange(change);
                }
                else
                {
                    if (EnumerateFiles(path, "*", SearchOption.AllDirectories).Any()
                        || EnumerateDirectories(path, "*", SearchOption.AllDirectories).Any())
                    {
                        throw new IOException(string.Format(MessageStrings.DirectoryNotEmpty, path));
                    }
                }
            }
            else
            {
                if (change != null && change.FileSystemChangeType == FileSystemChangeType.AddDirectory)
                {
                    // All changes here then should be just additions.
                    FileSystemChangeTracker.RemoveChanges(subDirectoryChanges);
                }
                else
                {
                    var files = EnumerateFiles(path, "*", SearchOption.AllDirectories);
                    foreach(var file in files)
                    {
                        DeleteFile(file);
                    }

                    var subDirs = EnumerateDirectories(path, "*", SearchOption.AllDirectories);
                    foreach(var subDir in subDirs)
                    {
                        RemoveDirectory(subDir, false);
                    }
                }
            }

        }

        private IEnumerable<string> EnumerateDirectories(string path, string searchPattern, SearchOption searchOption)
        {
            if(!DirectoryExists(path))
            {
                throw new IOException(string.Format(MessageStrings.PathNotFound, path));
            }

            var dirsOnDisk = Directory.Exists(path)
                ? Directory.EnumerateDirectories(path, searchPattern, searchOption)
                : new List<string>();

            IEnumerable<FileSystemChangeInformation> changedDirs = GetChangesFromDirectory(
                path,
                searchOption,
                f=>(f.FileSystemChangeType == FileSystemChangeType.AddDirectory || f.FileSystemChangeType == FileSystemChangeType.RemoveDirectory));

            List<string> enumeratedDirs = new List<string>(dirsOnDisk);

            foreach (var changedDir in changedDirs)
            {
                if (changedDir.FileSystemChangeType == FileSystemChangeType.AddDirectory
                    && MatchesPattern(Path.GetFileName(changedDir.FullPath), searchPattern))
                {
                    enumeratedDirs.Add(changedDir.FullPath);
                }
                else if (changedDir.FileSystemChangeType == FileSystemChangeType.RemoveDirectory)
                {
                    enumeratedDirs.Remove(changedDir.FullPath);
                }
            }

            return enumeratedDirs;
        }

        public IEnumerable<string> EnumerateFiles(string path, string searchPattern, SearchOption searchOption)
        {
            if (!DirectoryExists(path))
            {
                throw new IOException(string.Format(MessageStrings.PathNotFound, path));
            }

            var filesOnDisk = Directory.Exists(path)
                ? Directory.EnumerateFiles(path, searchPattern, searchOption)
                : new List<string>();

            IEnumerable<FileSystemChangeInformation> changedFiles = GetChangesFromDirectory(
                path,
                searchOption,
                f => f.FileSystemChangeType == FileSystemChangeType.AddFile || f.FileSystemChangeType == FileSystemChangeType.DeleteFile);

            List<string> enumeratedFiles = new List<string>(filesOnDisk);

            foreach (var changedFile in changedFiles)
            {
                if (changedFile.FileSystemChangeType == FileSystemChangeType.AddFile
                    && MatchesPattern(Path.GetFileName(changedFile.FullPath), searchPattern))
                {
                    enumeratedFiles.Add(changedFile.FullPath);
                }
                else if (changedFile.FileSystemChangeType == FileSystemChangeType.DeleteFile)
                {
                    enumeratedFiles.Remove(changedFile.FullPath);
                }

            }

            return enumeratedFiles;
        }
        #endregion

        private IEnumerable<FileSystemChangeInformation> GetChangesFromDirectory(
            string path,
            SearchOption searchOption,
            Func<FileSystemChangeInformation, bool> changeTypeFilter)
        {
            if (searchOption == SearchOption.AllDirectories)
            {
                return FileSystemChanges.Where(
                    f => Path.GetDirectoryName(f.FullPath)
                        .StartsWith(path, PathComparisonType)
                        && changeTypeFilter(f));
            }
            else
            {
                return FileSystemChanges.Where(
                    f => Path.GetDirectoryName(f.FullPath)
                        .Equals(path, PathComparisonType)
                        && changeTypeFilter(f));
            }
        }


        private bool MatchesPattern(string fileName, string searchPattern)
        {
            Regex rx = new Regex(Regex.Escape(searchPattern).Replace("\\*", ".*").Replace("\\?", "."));
            return rx.IsMatch(fileName);
        }
    }
}