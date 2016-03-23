// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.DotNet.ProjectModel.Compilation;
using Microsoft.Extensions.CodeGeneration.Sources.Test;
using Moq;
using Xunit;

namespace Microsoft.Extensions.CodeGeneration.DotNet
{
    public class LibraryExportExtensionsTest : TestBase
    {
        LibraryExport _export;
        public LibraryExportExtensionsTest()  
            : base(@"..\TestApps\ModelTypesLocatorTestClassLibrary")
        {
            var _libraryExporter = new LibraryExporter(_projectContext, _environment);
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
            //arrange
            var libraryExporter = GetInvalidLibraryExporter();
            _export = libraryExporter.GetAllExports().Last();
            
            //act
            try 
            {
                var references = LibraryExportExtensions.GetMetadataReferences(_export);
                Assert.True(false, "Expected to get an exception");
            }
            catch (Exception ex)
                when (ex is FileNotFoundException 
                            || ex is DirectoryNotFoundException
                            || ex is NotSupportedException
                            || ex is ArgumentException
                            || ex is ArgumentOutOfRangeException
                            || ex is BadImageFormatException
                            || ex is IOException
                            || ex is ArgumentNullException)
            {
                //We want to be here
                return;
            }
        }
        
        [Fact]
        public void LibraryExportExtensions_GetMetadataReferences_doNotThrowException()
        {
            //arrange
            var libraryExporter = GetInvalidLibraryExporter();
            _export = libraryExporter.GetAllExports().Last();
            
            //act
            try 
            {
                var references = LibraryExportExtensions.GetMetadataReferences(_export, throwOnError: false);
            }
            catch
            {
                Assert.True(false, "Exception while getting Metadata references even when throwOnError is set to false");
                //We want to be here
                return;
            }
            return;
        }
        
        private LibraryExporter GetInvalidLibraryExporter()
        {
            IApplicationEnvironment applicationEnvironment;
#if RELEASE 
            applicationEnvironment = new ApplicationEnvironment("ModelTypesLocatorTestClassLibrary", _projectPath, "Debug");
#else
            applicationEnvironment = new ApplicationEnvironment("ModelTypesLocatorTestClassLibrary", _projectPath, "Release");
#endif
            return new LibraryExporter(_projectContext, applicationEnvironment);
        }
    }
}