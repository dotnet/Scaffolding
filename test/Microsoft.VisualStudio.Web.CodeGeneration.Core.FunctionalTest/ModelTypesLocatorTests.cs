// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.VisualStudio.Web.CodeGeneration.DotNet;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Microsoft.VisualStudio.Web.CodeGeneration.Core.FunctionalTest
{
    public class ModelTypesLocatorTests
    {
        private readonly IServiceProvider _serviceProvider = TestHelper.CreateServices("ModelTypesLocatorTestWebApp");

        [Fact]
        public void GetType_Finds_Exact_Type_In_App()
        {
            //Arrange
            var locator = GetModelTypesLocator();
            //Act
            var types = locator.GetType("ModelTypesLocatorTestWebApp.Models.ModelWithMatchingShortName");
            
            //Assert
            Assert.Equal(1, types.Count());
            var type = types.First();
            Assert.Equal("ModelWithMatchingShortName", type.Name);
            Assert.Equal("ModelTypesLocatorTestWebApp.Models", type.Namespace);
            Assert.Equal("ModelTypesLocatorTestWebApp.Models.ModelWithMatchingShortName", type.FullName);
        }

        [Fact]
        public void GetType_Does_Not_Find_Type_From_A_Binary_Reference()
        {
            //Arrange
            var locator = GetModelTypesLocator();

            //Act
            var types = locator.GetType("System.Object");

            //Assert
            Assert.Equal(0, types.Count());
        }

        [Fact]
        public void GetType_Finds_Exact_Type_In_Referenced_ClassLib()
        {
            //Arrange
            var locator = GetModelTypesLocator();

            //Act
            var types = locator.GetType("ModelTypesLocatorTestClassLibrary.ModelWithMatchingShortName");

            //Assert
            Assert.Equal(1, types.Count());
            var type = types.First();
            Assert.Equal("ModelWithMatchingShortName", type.Name);
            Assert.Equal("ModelTypesLocatorTestClassLibrary", type.Namespace);

            types = locator.GetType("ClassLibrary1.Class1");

            //Assert
            Assert.Equal(1, types.Count());
            type = types.First();
            Assert.Equal("Class1", type.Name);
            Assert.Equal("ClassLibrary1", type.Namespace);
        }

        [Fact]
        public void GetType_Fallsback_To_Short_TypeName_Match()
        {
            //Arrange
            var locator = GetModelTypesLocator();

            //Act
            var types = locator.GetType("ModelWithMatchingShortName");

            //Assert
            Assert.Equal(2, types.Count());
        }

        [Fact]
        public void GetAllTypes_Gets_All_Types_Including_ReferencedProjects()
        {

            var services = TestHelper.CreateServices("ClassLibrary2");

            //Arrange
            //var locator = GetModelTypesLocator();
            var locator = new ModelTypesLocator((ILibraryExporter)services.GetRequiredService(typeof(ILibraryExporter)),
                (Workspace)services.GetRequiredService(typeof(Workspace)));

            //Act
            var types = locator.GetAllTypes();

            //Assert
            Assert.Equal(3, types.Count());
        }

        private ModelTypesLocator GetModelTypesLocator()
        {
            return new ModelTypesLocator(
                (ILibraryExporter)_serviceProvider.GetRequiredService(typeof(ILibraryExporter)),
                (Workspace)_serviceProvider.GetRequiredService(typeof(Workspace)));
        }
    }
}