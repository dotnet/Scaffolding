// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.VisualStudio.Web.CodeGeneration.Test.Sources;
using Xunit;

namespace Microsoft.VisualStudio.Web.CodeGeneration.Core.Test
{
    public class FilesLocatorTests
    {
        [Fact]
        public void FilesLocator_Returns_Entry_From_FirstSearchPath()
        {
            MockFileSystem fs = new MockFileSystem();
            var folders = new[] { @"C:\One", @"C:\Two" };
            fs.AddFolders(folders);

            fs.WriteAllText(@"C:\One\template.cshtml", "");
            fs.WriteAllText(@"C:\Two\template.cshtml", "");
            FilesLocator locator = new FilesLocator(fs);

            var result = locator.GetFilePath("template.cshtml", folders);

            Assert.Equal(@"C:\One\template.cshtml", result);
        }

        [Fact]
        public void FilesLocator_Throws_When_Multiple_Matches_In_OneSearchPath()
        {
            MockFileSystem fs = new MockFileSystem();
            var folders = new[] { @"C:\One", @"C:\One\Sub1", @"C:\One\Sub2" };
            fs.AddFolders(folders);

            fs.WriteAllText(@"C:\One\Sub1\template.cshtml", "");
            fs.WriteAllText(@"C:\One\Sub2\template.cshtml", "");
            FilesLocator locator = new FilesLocator(fs);

            var ex = Assert.Throws<InvalidOperationException>(() => locator.GetFilePath("template.cshtml", new[] { @"C:\One" }));
            Assert.Equal(@"Multiple files with name template.cshtml found within C:\One", ex.Message);
        }

        [Fact]
        public void FilesLocator_Throws_When_No_Matches_In_SearchPaths()
        {
            MockFileSystem fs = new MockFileSystem();
            var folders = new[] { @"C:\One", @"C:\Two" };
            fs.AddFolders(folders);

            fs.WriteAllText(@"C:\One\template1.cshtml", "");
            fs.WriteAllText(@"C:\Two\template2.cshtml", "");
            FilesLocator locator = new FilesLocator(fs);

            var ex = Assert.Throws<InvalidOperationException>(() => locator.GetFilePath("template.cshtml", folders));
            Assert.Equal(@"A file matching the name 'template.cshtml' was not found within any of the folders: C:\One;C:\Two", ex.Message);
        }

        [Fact]
        public void FilesLocator_Throws_When_No_SearchPaths_Does_Not_Exist()
        {
            FilesLocator locator = new FilesLocator(new MockFileSystem());

            var ex = Assert.Throws<InvalidOperationException>(() => locator.GetFilePath("template.cshtml", new[] { @"C:\One", @"C:\Two" }));
            Assert.Equal(@"A file matching the name 'template.cshtml' was not found within any of the folders: [C:\One;C:\Two]", ex.Message);
        }
    }
}
