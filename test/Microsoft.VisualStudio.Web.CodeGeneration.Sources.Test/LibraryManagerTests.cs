// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Linq;
using Microsoft.VisualStudio.Web.CodeGeneration.DotNet;
using Xunit;
using System.IO;

namespace Microsoft.VisualStudio.Web.CodeGeneration.Sources.Test
{
    public class LibraryManagerTests : TestBase
    {
#if NET451
        static string testAppPath = Path.Combine("..", "..", "..", "..", "..", "TestApps", "ModelTypesLocatorTestClassLibrary");
#else
        static string testAppPath = Path.Combine("..", "TestApps", "ModelTypesLocatorTestClassLibrary");
#endif
        LibraryManager _libraryManager;

        public LibraryManagerTests()
            : base(Path.Combine(testAppPath))
        {
        }

        [Fact]
        public void LibraryManager_TestGetLibraries()
        {
            //Arrange
            _libraryManager = new LibraryManager(_projectContext);

            //Act
            var libraries = _libraryManager.GetLibraries();

            //Assert
            Assert.True(libraries.Where(_ => _.Identity.Name == "ModelTypesLocatorTestClassLibrary").Any());
        }

        [Fact]
        public void LibraryManager_TestGetLibrary()
        {
            //Arrange
            _libraryManager = new LibraryManager(_projectContext);

            //Act
            var foundLibrary = _libraryManager.GetLibrary("ModelTypesLocatorTestClassLibrary");
            var notFoundLibrary = _libraryManager.GetLibrary("XXX");
            

            //Assert
            Assert.Equal("ModelTypesLocatorTestClassLibrary", foundLibrary.Identity.Name);
            Assert.Null(notFoundLibrary);
        }

        [Fact]
        public void LibraryManager_TestGetReferencingLibraries()
        {
            //Arrange
            _libraryManager = new LibraryManager(_projectContext);

            //Act
            var x = _libraryManager.GetReferencingLibraries("Microsoft.VisualStudio.Web.CodeGeneration");

            //Assert
            Assert.True(x.Where(_ => _.Identity.Name == "Microsoft.VisualStudio.Web.CodeGenerators.Mvc").Any());
        }
    }
}
