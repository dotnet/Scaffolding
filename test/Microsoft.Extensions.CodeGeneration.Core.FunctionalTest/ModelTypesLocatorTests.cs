// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using Microsoft.Extensions.CodeGeneration.DotNet;
using Microsoft.CodeAnalysis;

namespace Microsoft.Extensions.CodeGeneration.Core.FunctionalTest
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

        [Fact(Skip = "this test now 39 types including all the Hosting services added and internal not null attribute??")]
        public void GetAllTypes_Gets_All_Types_Including_ReferencedProjects()
        {
            //Arrange
            var locator = GetModelTypesLocator();

            //Act
            var types = locator.GetAllTypes();

            //Assert
            Assert.Equal(3, types.Count());
        }

        private ModelTypesLocator GetModelTypesLocator()
        {
            return new ModelTypesLocator(
                (ILibraryExporter)_serviceProvider.GetRequiredService(typeof(ILibraryExporter)),
                (IApplicationEnvironment)_serviceProvider.GetRequiredService(typeof(IApplicationEnvironment)),
                (Workspace)_serviceProvider.GetRequiredService(typeof(Workspace)));
        }
    }
}