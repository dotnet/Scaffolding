// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.VisualStudio.Web.CodeGeneration.Contracts.FileSystemChange;
using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
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
        public void SimulationModeFileSystem_AddFile()
        {
            var fileSystem = new SimulationModeFileSystem();
            var contents = "DummyContents";
            byte[] bytes = Encoding.UTF8.GetBytes(contents);

            fileSystem.AddFileAsync("DummyOutputPath", new MemoryStream(bytes));

            Assert.Equal("DummyOutputPath", fileSystem.FileSystemChanges.First().FullPath);
            Assert.Equal("DummyContents", fileSystem.FileSystemChanges.First().FileContents);
            Assert.Equal(FileSystemChangeType.AddFile, fileSystem.FileSystemChanges.First().FileSystemChangeType);
        }

        [Fact]
        public void SimulationModeFileSystem_EditFile()
        {
            var fileSystem = new SimulationModeFileSystem();
            var path = Path.GetTempFileName();
            var contents = "DummyContents";
            byte[] bytes = Encoding.UTF8.GetBytes(contents);

            fileSystem.AddFileAsync(path, new MemoryStream(bytes));

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
    }
}
