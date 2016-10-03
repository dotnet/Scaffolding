// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using Xunit;

namespace Microsoft.VisualStudio.Web.CodeGeneration.Core.FunctionalTest
{
    public class ModelTypesLocatorTests
    {
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

        [Fact()]
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
            //Arrange
            var locator = GetModelTypesLocator();

            //Act
            var types = locator.GetAllTypes();

            //Assert
            Assert.Equal(4, types.Count());
        }

        private ModelTypesLocator GetModelTypesLocator()
        {
            return new ModelTypesLocator(GetTestWorkspace());
        }

        private Workspace GetTestWorkspace()
        {
            var workspace = new AdhocWorkspace();
            /*
                Dependency:
                TestAssembly -> ClassLib1 -> ClassLib2
            */
            var classLibrary2 = CodeAnalysis.ProjectInfo.Create(ProjectId.CreateNewId(),
                VersionStamp.Default,
                "ClassLibrary2",
                "ClassLibrary2",
                LanguageNames.CSharp);

            var classLib2Project = workspace.AddProject(classLibrary2);

            var classLibrary1 = CodeAnalysis.ProjectInfo.Create(ProjectId.CreateNewId(),
                VersionStamp.Default,
                "ClassLibrary1",
                "ClassLibrary1",
                LanguageNames.CSharp,
                projectReferences: new ProjectReference[] { new ProjectReference(classLib2Project.Id) });

            var classLib1Project = workspace.AddProject(classLibrary1);

            var projectInfo = CodeAnalysis.ProjectInfo.Create(ProjectId.CreateNewId(),
                VersionStamp.Default,
                "TestAssembly",
                "TestAssembly",
                LanguageNames.CSharp,
                projectReferences: new ProjectReference[] { new ProjectReference(classLib1Project.Id) });

            var project = workspace.AddProject(projectInfo);

            var modelWithMatchingShortName = SourceText.From("namespace ModelTypesLocatorTestClassLibrary { class ModelWithMatchingShortName { } } ");
            workspace.AddDocument(project.Id, "ModelWithMatchingShortName.cs", modelWithMatchingShortName);

            var modelWithMatchingShortName2 = SourceText.From("namespace ModelTypesLocatorTestWebApp.Models { class ModelWithMatchingShortName { } }");
            workspace.AddDocument(project.Id, "ModelWithMatchingShortName.cs", modelWithMatchingShortName2);

            var class1 = SourceText.From("namespace ClassLibrary1 { class Class1 { } }");
            workspace.AddDocument(classLib1Project.Id, "Class1.cs", class1);

            var class2 = SourceText.From("namespace ClassLibrary2 { class Class1 { } }");
            workspace.AddDocument(classLib2Project.Id, "Class1.cs", class2);

            return workspace;
        }
    }
}