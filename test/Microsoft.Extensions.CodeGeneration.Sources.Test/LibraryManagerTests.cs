using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.CodeGeneration.DotNet;
using Microsoft.DotNet.ProjectModel;
using Microsoft.DotNet.ProjectModel.Graph;
using Moq;
using Xunit;
using System.IO;

namespace Microsoft.Extensions.CodeGeneration.Sources.Test
{
    public class LibraryManagerTests : TestBase
    {
        LibraryManager _libraryManager;

        public LibraryManagerTests() : base(@"..\TestApps\ModelTypesLocatorTestClassLibrary")
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
            var x = _libraryManager.GetReferencingLibraries("Microsoft.Extensions.CodeGeneration");

            //Assert
            Assert.True(x.Where(_ => _.Identity.Name == "Microsoft.Extensions.CodeGenerators.Mvc").Any());
        }
    }
}
