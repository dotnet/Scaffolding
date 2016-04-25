// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.DotNet.ProjectModel.Compilation;
using Microsoft.Extensions.CodeGeneration.Sources.Test;
using Xunit;

namespace Microsoft.Extensions.CodeGeneration.DotNet
{
    public class LibraryExportExtensionsTest : TestBase
    {
        LibraryExport _export;
        public LibraryExportExtensionsTest()  
            : base(Path.Combine("..", "TestApps", "ModelTypesLocatorTestClassLibrary"))
        {
            var _libraryExporter = new LibraryExporter(_projectContext, _applicationInfo);
            _export = _libraryExporter.GetAllExports().First();
        }
        
        [Fact]
        public void LibraryExportExtensions_GetMetadataReferences()
        {
            var refList = _export.GetMetadataReferences();
            Assert.True(refList.Any());
        }

        [Fact]
        public void LibraryExportExtensions_GetMetadataReferences_throwException()
        {
            var libraryExporter = GetInvalidLibraryExporter();
            _export = libraryExporter.GetExport("ModelTypesLocatorTestClassLibrary");

            var ex = Assert.ThrowsAny<Exception>(() => LibraryExportExtensions.GetMetadataReferences(_export));
            Assert.True(ex is FileNotFoundException
                            || ex is DirectoryNotFoundException
                            || ex is NotSupportedException
                            || ex is ArgumentException
                            || ex is ArgumentOutOfRangeException
                            || ex is BadImageFormatException
                            || ex is IOException
                            || ex is ArgumentNullException);
        }
        
        [Fact]
        public void LibraryExportExtensions_GetMetadataReferences_doNotThrowException()
        {
            var libraryExporter = GetInvalidLibraryExporter();
            _export = libraryExporter.GetExport("ModelTypesLocatorTestClassLibrary");

            LibraryExportExtensions.GetMetadataReferences(_export, throwOnError: false);
        }
        
        private LibraryExporter GetInvalidLibraryExporter()
        {
            IApplicationInfo applicationInfo;
            applicationInfo = new ApplicationInfo("ModelTypesLocatorTestClassLibrary", _projectPath, "NonExistent");
            return new LibraryExporter(_projectContext, applicationInfo);
        }
    }
}
