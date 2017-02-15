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

            Assert.Equal(afterDbContextText, result.NewTree.GetText().ToString());
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

            var result = testObj.EditStartupForNewContext(startupType, "MyContext", "ContextNamespace", "MyContext-NewGuid");

            Assert.True(result.Edited);
            Assert.Equal(afterStartupText, result.NewTree.GetText().ToString());
        }

        [Fact]
        public void AddConnectionString_Creates_App_Settings_File()
        {
            //Arrange
            var fs = new MockFileSystem();
            var testObj = GetTestObject(fs);

            //Act
            testObj.AddConnectionString("MyDbContext", "MyDbContext-NewGuid");

            //Assert
            string expected = @"{
  ""ConnectionStrings"": {
    ""MyDbContext"": ""Server=(localdb)\\mssqllocaldb;Database=MyDbContext-NewGuid;Trusted_Connection=True;MultipleActiveResultSets=true""
  }
}";
            var appSettingsPath = Path.Combine(AppBase, "appsettings.json"); 
            fs.FileExists(appSettingsPath);
            Assert.Equal(expected, fs.ReadAllText(appSettingsPath));
        }

        [Theory]
        // Empty invalid json file - should this be supported?
        //[InlineData("",
        //    "{\r\n  \"Data\": {\r\n    \"MyDbContext\": {\r\n      \"ConnectionString\": \"@\\\"Server=(localdb)\\\\mssqllocaldb;Database=MyDbContext-NewGuid;Trusted_Connection=True;MultipleActiveResultSets=true\\\"\"\r\n    }\r\n  }\r\n}")]
        // Empty file with valid json token
        [InlineData("{}",
                    @"{
  ""ConnectionStrings"": {
    ""MyDbContext"": ""Server=(localdb)\\mssqllocaldb;Database=MyDbContext-NewGuid;Trusted_Connection=True;MultipleActiveResultSets=true""
  }
}")]
        // File with no node for connection name
        [InlineData(@"{
  ""ConnectionStrings"": {
  }
}",
                    @"{
  ""ConnectionStrings"": {
    ""MyDbContext"": ""Server=(localdb)\\mssqllocaldb;Database=MyDbContext-NewGuid;Trusted_Connection=True;MultipleActiveResultSets=true""
  }
}")]
        // File with node for connection name and also existing ConnectionString property
        // modification should be skipped in this case
        [InlineData(@"{
  ""ConnectionStrings"": {
    ""MyDbContext"": ""SomeExistingValue""
  }
}",
                     @"{
  ""ConnectionStrings"": {
    ""MyDbContext"": ""SomeExistingValue""
  }
}")]
        public void AddConnectionString_Modifies_App_Settings_File_As_Required(string previousContent, string newContent)
        {
            //Arrange
            var fs = new MockFileSystem();
            var appSettingsPath = Path.Combine(AppBase, "appsettings.json");
            fs.WriteAllText(appSettingsPath, previousContent);
            var testObj = GetTestObject(fs);

            //Act
            testObj.AddConnectionString("MyDbContext", "MyDbContext-NewGuid");

            //Assert
            Assert.Equal(newContent, fs.ReadAllText(appSettingsPath));
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
                fs != null ? fs : new MockFileSystem());
        }

        private static readonly string AppBase = "AppBase";
    }
}
