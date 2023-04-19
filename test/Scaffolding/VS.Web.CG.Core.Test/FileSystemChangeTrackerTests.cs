// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Linq;
using Microsoft.DotNet.Scaffolding.Shared;
using Xunit;

namespace Microsoft.VisualStudio.Web.CodeGeneration.Core.Test
{
    public class FileSystemChangeTrackerTests
    {
        [Fact]
        public void FileSystemChangeTracker_Returns_Last_change()
        {
            var changeTracker = new FileSystemChangeTracker();
            changeTracker.AddChange(new FileSystemChangeInformation()
            {
                FileContents = "abc",
                FullPath = "DummyPath",
                FileSystemChangeType = FileSystemChangeType.AddFile
            });

            Assert.Single(changeTracker.Changes);
            Assert.Equal("DummyPath", changeTracker.Changes.First().FullPath);
            Assert.Equal("abc", changeTracker.Changes.First().FileContents);

            changeTracker.AddChange(new FileSystemChangeInformation()
            {
                FileContents = "def",
                FullPath = "DummyPath",
                FileSystemChangeType = FileSystemChangeType.AddFile
            });

            Assert.Single(changeTracker.Changes);
            Assert.Equal("DummyPath", changeTracker.Changes.First().FullPath);
            Assert.Equal("def", changeTracker.Changes.First().FileContents);
        }

        [Fact]
        public void FileSystemChangeTracker_Returns_changes()
        {
            var changeTracker = new FileSystemChangeTracker();
            changeTracker.AddChange(new FileSystemChangeInformation()
            {
                FileContents = "abc",
                FullPath = "DummyPath1",
                FileSystemChangeType = FileSystemChangeType.AddFile
            });

            Assert.Single(changeTracker.Changes);
            Assert.Equal("DummyPath1", changeTracker.Changes.First().FullPath);
            Assert.Equal("abc", changeTracker.Changes.First().FileContents);

            changeTracker.AddChange(new FileSystemChangeInformation()
            {
                FileContents = "def",
                FullPath = "DummyPath2",
                FileSystemChangeType = FileSystemChangeType.EditFile
            });

            Assert.Equal(2, changeTracker.Changes.Count());
            Assert.Equal("DummyPath2", changeTracker.Changes.First(p => p.FullPath.Equals("DummyPath2")).FullPath);
            Assert.Equal("def", changeTracker.Changes.First(p => p.FullPath.Equals("DummyPath2")).FileContents);
        }

        [Fact]
        public void FileSystemChangeTracker_ClearChanges()
        {
            var changeTracker = new FileSystemChangeTracker();
            changeTracker.AddChange(new FileSystemChangeInformation()
            {
                FileContents = "abc",
                FullPath = "DummyPath1",
                FileSystemChangeType = FileSystemChangeType.AddFile
            });

            Assert.Single(changeTracker.Changes);
            Assert.Equal("DummyPath1", changeTracker.Changes.First().FullPath);
            Assert.Equal("abc", changeTracker.Changes.First().FileContents);

            changeTracker.ClearChanges();
            Assert.Empty(changeTracker.Changes);
        }
    }
}
