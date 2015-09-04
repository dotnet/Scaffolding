using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.Data.Entity;
using Microsoft.Dnx.Runtime;
using Microsoft.Framework.CodeGeneration.Templating;
using Moq;
using Xunit;
using System.Diagnostics;

namespace Microsoft.Framework.CodeGeneration.EntityFramework.Test
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

            Assert.True(result.Added);
            Assert.Equal(afterDbContextText, result.NewTree.GetText().ToString());
        }
    }
}
