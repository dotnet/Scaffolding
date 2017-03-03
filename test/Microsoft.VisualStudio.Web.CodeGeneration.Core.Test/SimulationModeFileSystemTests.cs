// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using Microsoft.VisualStudio.Web.CodeGeneration.Contracts.FileSystemChange;
using Xunit;

namespace Microsoft.VisualStudio.Web.CodeGeneration.Core.Test
{
    public class SimulationModeFileSystemTests
    {
        [Fact]
        public void SimulationModeFileSystem_CreateDirectory()
        {
            var fileSystem = new SimulationModeFileSystem();
            fileSystem.CreateDirectory("DummyPath");
            Assert.Equal(1, fileSystem.FileSystemChanges.Count());
            Assert.Equal(FileSystemChangeType.AddDirectory, fileSystem.FileSystemChanges.First().FileSystemChangeType);

            // Add existing directory doesn't add a change.
            fileSystem.CreateDirectory(Directory.GetCurrentDirectory());
            Assert.Equal(1, fileSystem.FileSystemChanges.Count());

        }

        [Fact]
        public void SimulationModeFileSystem_MakeFileWritable()
        {
            var fileSystem = new SimulationModeFileSystem();
            fileSystem.MakeFileWritable(typeof(SimulationModeFileSystemTests).GetTypeInfo().Assembly.Location);
            Assert.Equal(0, fileSystem.FileSystemChanges.Count());
        }

        [Fact]
        public async void SimulationModeFileSystem_AddFile()
        {
            var fileSystem = new SimulationModeFileSystem();
            var contents = "DummyContents";
            byte[] bytes = Encoding.UTF8.GetBytes(contents);

            await fileSystem.AddFileAsync("DummyOutputPath", new MemoryStream(bytes));

            Assert.Equal("DummyOutputPath", fileSystem.FileSystemChanges.First().FullPath);
            Assert.Equal("DummyContents", fileSystem.FileSystemChanges.First().FileContents);
            Assert.Equal(FileSystemChangeType.AddFile, fileSystem.FileSystemChanges.First().FileSystemChangeType);
        }

        [Fact]
        public async void SimulationModeFileSystem_EditFile()
        {
            var fileSystem = new SimulationModeFileSystem();
            var path = Path.GetTempFileName();
            var contents = "DummyContents";
            byte[] bytes = Encoding.UTF8.GetBytes(contents);

            await fileSystem.AddFileAsync(path, new MemoryStream(bytes));

            Assert.Equal(path, fileSystem.FileSystemChanges.First().FullPath);
            Assert.Equal("DummyContents", fileSystem.FileSystemChanges.First().FileContents);
            Assert.Equal(FileSystemChangeType.EditFile, fileSystem.FileSystemChanges.First().FileSystemChangeType);

            try
            {
                File.Delete(path);
            }
            catch
            {
                // do nothing.
            }
        }

        [Fact]
        public async void SimulationModeFileSystem_EnumerateFiles_AddedFiles()
        {
            var fileSystem = new SimulationModeFileSystem();
            var dirPath = Directory.GetCurrentDirectory();
            var expected = Directory.EnumerateFiles(dirPath, "*.*", SearchOption.TopDirectoryOnly);
            var actual = fileSystem.EnumerateFiles(dirPath, "*.*", SearchOption.TopDirectoryOnly);

            Assert.Equal(expected, actual);

            var contents = "DummyContents";
            byte[] bytes = Encoding.UTF8.GetBytes(contents);

            await fileSystem.AddFileAsync(Path.Combine(dirPath, "DummyOutputPath.txt"), new MemoryStream(bytes));

            actual = fileSystem.EnumerateFiles(dirPath, "*.*", SearchOption.TopDirectoryOnly);
            Assert.True(actual.Any(f => f.Equals(Path.Combine(dirPath, "DummyOutputPath.txt"))));

            actual = fileSystem.EnumerateFiles(dirPath, "DummyOutputPat?.txt", SearchOption.TopDirectoryOnly);
            Assert.True(actual.Any(f => f.Equals(Path.Combine(dirPath, "DummyOutputPath.txt"))));

            actual = fileSystem.EnumerateFiles(dirPath, "*OutputPath.t?t", SearchOption.TopDirectoryOnly);
            Assert.True(actual.Any(f => f.Equals(Path.Combine(dirPath, "DummyOutputPath.txt"))));

            fileSystem.CreateDirectory(Path.Combine(dirPath, "test_dir"));
            await fileSystem.AddFileAsync(Path.Combine(dirPath, "test_dir", "TestFile.abc"), new MemoryStream(bytes));

            actual = fileSystem.EnumerateFiles(dirPath, "*.*", SearchOption.AllDirectories);
            Assert.True(actual.Any(f => f.Equals(Path.Combine(dirPath, "DummyOutputPath.txt"))));
            Assert.True(actual.Any(f => f.Equals(Path.Combine(dirPath, "test_dir", "TestFile.abc"))));

            actual = fileSystem.EnumerateFiles(dirPath, "*.abc", SearchOption.AllDirectories);
            Assert.False (actual.Any(f => f.Equals(Path.Combine(dirPath, "DummyOutputPath.txt"))));
            Assert.True(actual.Any(f => f.Equals(Path.Combine(dirPath, "test_dir", "TestFile.abc"))));
        }

        [Fact]
        public void SimulationModeFileSystem_EnumerateFiles_RemovedFiles()
        {
            var fileSystem = new SimulationModeFileSystem();
            var dirPath = Path.GetTempPath();
            dirPath = Directory.CreateDirectory(Guid.NewGuid().ToString()).FullName;

            var contents = "DummyContents";
            byte[] bytes = Encoding.UTF8.GetBytes(contents);

            File.WriteAllBytes(Path.Combine(dirPath, "test_file.txt"), bytes);

            var expected = Directory.EnumerateFiles(dirPath, "*.*", SearchOption.TopDirectoryOnly);
            var actual = fileSystem.EnumerateFiles(dirPath, "*.*", SearchOption.TopDirectoryOnly);

            Assert.Equal(expected, actual);
            Assert.True(actual.Any(f => f.Equals(Path.Combine(dirPath, "test_file.txt"))));
            File.Delete(Path.Combine(dirPath, "test_file.txt"));

            actual = fileSystem.EnumerateFiles(dirPath, "*.*", SearchOption.TopDirectoryOnly);

            Assert.False(actual.Any(f => f.Equals(Path.Combine(dirPath, "test_file.txt"))));
        }
    }
}
