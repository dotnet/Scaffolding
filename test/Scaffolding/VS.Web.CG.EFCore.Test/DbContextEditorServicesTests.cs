// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.IO;
using System.Linq;
using System.Reflection;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.VisualStudio.Web.CodeGeneration.Contracts.ProjectModel;
using Microsoft.VisualStudio.Web.CodeGeneration.DotNet;
using Microsoft.VisualStudio.Web.CodeGeneration.EntityFrameworkCore;
using Microsoft.VisualStudio.Web.CodeGeneration.EntityFrameworkCore.Test;
using Microsoft.VisualStudio.Web.CodeGeneration.Templating;
using Microsoft.VisualStudio.Web.CodeGeneration.Test.Sources;
using Moq;
using Xunit;

namespace Microsoft.VisualStudio.Web.CodeGeneration
{
    public class DbContextEditorServicesTests
    {
        [Theory]
        [InlineData("DbContext_Before.txt", "MyModel.txt", "DbContext_After.txt")]
        public void AddModelToContext_Adds_Model_From_Same_Project_To_Context(string beforeContextResource, string modelResource, string afterContextResource)
        {
            string resourcePrefix = "compiler/resources/";

            var beforeDbContextText = ResourceUtilities.GetEmbeddedResourceFileContent(resourcePrefix + beforeContextResource);
            var modelText = ResourceUtilities.GetEmbeddedResourceFileContent(resourcePrefix + modelResource);
            var afterDbContextText = ResourceUtilities.GetEmbeddedResourceFileContent(resourcePrefix + afterContextResource);

            var contextTree = CSharpSyntaxTree.ParseText(beforeDbContextText);
            var modelTree = CSharpSyntaxTree.ParseText(modelText);
            var efReference = MetadataReference.CreateFromFile(Assembly.GetEntryAssembly().Location);

            var compilation = CSharpCompilation.Create("DoesNotMatter", new[] { contextTree, modelTree }, new[] { efReference });

            DbContextEditorServices testObj = GetTestObject();

            var types = RoslynUtilities.GetDirectTypesInCompilation(compilation);
            var modelType = ModelType.FromITypeSymbol(types.Where(ts => ts.Name == "MyModel").First());
            var contextType = ModelType.FromITypeSymbol(types.Where(ts => ts.Name == "MyContext").First());

            var result = testObj.AddModelToContext(contextType, modelType);

            Assert.True(result.Edited);

            Assert.Equal(afterDbContextText, result.NewTree.GetText().ToString(), ignoreCase: false, ignoreLineEndingDifferences: true);
        }

        [Theory]
        [InlineData("Startup_RegisterContext_Before.txt", "Startup_RegisterContext_After.txt", "DbContext_Before.txt")]
        [InlineData("Startup_Empty_Method_RegisterContext_Before.txt", "Startup_Empty_Method_RegisterContext_After.txt", "DbContext_Before.txt")]
        public void TryEditStartupForNewContext_Adds_Context_Registration_To_ConfigureServices(string beforeStartupResource, string afterStartupResource, string dbContextResource)
        {
            string resourcePrefix = "compiler/resources/";

            var beforeStartupText = ResourceUtilities.GetEmbeddedResourceFileContent(resourcePrefix + beforeStartupResource);
            var afterStartupText = ResourceUtilities.GetEmbeddedResourceFileContent(resourcePrefix + afterStartupResource);
            var dbContextText = ResourceUtilities.GetEmbeddedResourceFileContent(resourcePrefix + dbContextResource);
            var startupTree = CSharpSyntaxTree.ParseText(beforeStartupText);
            var contextTree = CSharpSyntaxTree.ParseText(dbContextText);
            var testAssembly =
                Assembly.Load(new AssemblyName("Microsoft.VisualStudio.Web.CodeGeneration.EntityFrameworkCore.Test"));
            var configAssembly =
                Assembly.Load(new AssemblyName("Microsoft.Extensions.Configuration.Abstractions"));
            var efReference = MetadataReference.CreateFromFile(testAssembly.Location);
            
            var configReference = MetadataReference.CreateFromFile(configAssembly.Location);

            
            var compilation = CSharpCompilation.Create("DoesNotMatter", new[] { startupTree, contextTree }, new[] { efReference, configReference });

            DbContextEditorServices testObj = GetTestObject();

            var types = RoslynUtilities.GetDirectTypesInCompilation(compilation);
            var startupType = ModelType.FromITypeSymbol(types.Where(ts => ts.Name == "Startup").First());
            var contextType = ModelType.FromITypeSymbol(types.Where(ts => ts.Name == "MyContext").First());

            var result = testObj.EditStartupForNewContext(startupType, "MyContext", "ContextNamespace", "MyContext-NewGuid", false);

            Assert.True(result.Edited);
            Assert.Equal(afterStartupText, result.NewTree.GetText().ToString(), ignoreCase: false, ignoreLineEndingDifferences: true);
        }


        private DbContextEditorServices GetTestObject(MockFileSystem fs = null)
        {
            var app = new Mock<IApplicationInfo>();
            app.Setup(a => a.ApplicationBasePath).Returns(AppBase);
            return new DbContextEditorServices(
                new Mock<IProjectContext>().Object,
                app.Object,
                new Mock<IFilesLocator>().Object,
                new Mock<ITemplating>().Object,
                new Mock<IConnectionStringsWriter>().Object,
                fs != null ? fs : new MockFileSystem());
        }

        private static readonly string AppBase = "AppBase";
    }
}
