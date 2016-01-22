using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.PlatformAbstractions;
using Microsoft.Extensions.CodeGeneration.Templating;
using Moq;
using Xunit;

namespace Microsoft.Extensions.CodeGeneration.EntityFrameworkCore.Test
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
            var efReference = MetadataReference.CreateFromFile(typeof(DbContext).Assembly.Location);

            var compilation = CSharpCompilation.Create("DoesNotMatter", new[] { contextTree, modelTree }, new[] { efReference });

            DbContextEditorServices testObj = new DbContextEditorServices(
                new Mock<ILibraryManager>().Object,
                new Mock<IApplicationEnvironment>().Object,
                new Mock<IFilesLocator>().Object,
                new Mock<ITemplating>().Object);

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
            var efReference = MetadataReference.CreateFromFile(typeof(DbContext).Assembly.Location);

            var compilation = CSharpCompilation.Create("DoesNotMatter", new[] { startupTree, contextTree }, new[] { efReference });

            DbContextEditorServices testObj = new DbContextEditorServices(
                new Mock<ILibraryManager>().Object,
                new Mock<IApplicationEnvironment>().Object,
                new Mock<IFilesLocator>().Object,
                new Mock<ITemplating>().Object);

            var types = RoslynUtilities.GetDirectTypesInCompilation(compilation);
            var startupType = ModelType.FromITypeSymbol(types.Where(ts => ts.Name == "Startup").First());
            var contextType = ModelType.FromITypeSymbol(types.Where(ts => ts.Name == "MyContext").First());

            var result = testObj.EditStartupForNewContext(startupType, "MyContext", "ContextNamespace", "MyContext-NewGuid");

            Assert.True(result.Edited);
            Assert.Equal(afterStartupText, result.NewTree.GetText().ToString());
        }
    }
}
