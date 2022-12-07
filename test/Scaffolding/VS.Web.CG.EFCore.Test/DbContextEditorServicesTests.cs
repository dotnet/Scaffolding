// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.DotNet.Scaffolding.Shared;
using Microsoft.DotNet.Scaffolding.Shared.Project;
using Microsoft.DotNet.Scaffolding.Shared.ProjectModel;
using Microsoft.EntityFrameworkCore;
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
        private const string MinimalProgramCsFile = @"
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.
            builder.Services.AddRazorPages();

            var app = builder.Build();

            // Configure the HTTP request pipeline.
            if (!app.Environment.IsDevelopment())
            {
                app.UseExceptionHandler(""/Error"");
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }

            app.UseHttpsRedirection();
            app.UseStaticFiles();

            app.UseRouting();

            app.UseAuthorization();

            app.MapRazorPages();

            app.Run();
            ";

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

            var result = testObj.AddModelToContext(contextType, modelType, new Dictionary<string, string>() { { "nullableEnabled", bool.FalseString } });

            Assert.True(result.Edited);

            Assert.Equal(afterDbContextText, result.NewTree.GetText().ToString(), ignoreCase: false, ignoreLineEndingDifferences: true);
        }

        [Theory]
        [InlineData("DbContext_Before.txt", "MyModel.txt", "DbContextNullable_After.txt")]
        public void AddModelToContext_Adds_Model_From_Same_Project_To_Context_With_Nullable_Enabled(string beforeContextResource, string modelResource, string afterContextResource)
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

            var result = testObj.AddModelToContext(contextType, modelType, new Dictionary<string, string>() { { "nullableEnabled", bool.TrueString } });

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

            var result = testObj.EditStartupForNewContext(startupType, "MyContext", "ContextNamespace", "MyContext-NewGuid", false, false);

            Assert.True(result.Edited);
            Assert.Equal(afterStartupText, result.NewTree.GetText().ToString(), ignoreCase: false, ignoreLineEndingDifferences: true);
        }

        [Fact]
        public async Task GetAddDbContextStatementTests()
        {
            DbContextEditorServices testObj = GetTestObject();
            var syntaxTree = CSharpSyntaxTree.ParseText(MinimalProgramCsFile);
            var root = await syntaxTree.GetRootAsync();
            var dbContextExpression = testObj.GetAddDbContextStatement(root, "DbContextName", "DatabaseName", DbProvider.SqlServer);
            var correctDbContextString = "builder.Services.AddDbContext<DbContextName>(options => options.UseSqlServer(builder.Configuration.GetConnectionString(\"DbContextName\") ?? throw new InvalidOperationException(\"Connection string 'DbContextName' not found.\")));";

            var trimmedDbContextString = ProjectModifierHelper.TrimStatement(dbContextExpression.ToString());
            var trimmedCorrectDbContextString = ProjectModifierHelper.TrimStatement(correctDbContextString);
            Assert.Equal(trimmedCorrectDbContextString, trimmedDbContextString);
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
