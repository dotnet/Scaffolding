using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.DotNet.MSIdentity.AuthenticationParameters;
using Microsoft.DotNet.MSIdentity.CodeReaderWriter;
using Microsoft.DotNet.Scaffolding.Shared.Project;
using Microsoft.DotNet.MSIdentity.Tool;
using Xunit;
using ConsoleLogger = Microsoft.DotNet.MSIdentity.Tool.ConsoleLogger;

namespace Microsoft.DotNet.MSIdentity.UnitTests.Tests
{

    public class ProjectModifierTests : DocumentBuilderTestBase
    {
        [Theory]
        [InlineData(new object[] { new string[] { "Startup.cs", "File.cs", "Test", "", null},
                                   new string[] { "Startup", "File", "", "", "" } })]
        public void GetClassNameTests(string[] classNames, string[] formattedClassNames)
        {
            for (int i = 0; i < classNames.Length; i++)
            {
                string className = classNames[i];
                string formattedClassName = formattedClassNames[i];
                Assert.Equal(ProjectModifierHelper.GetClassName(className), formattedClassName);
            }
        }

        [Fact]
        public async Task GetStartupClassNameTests()
        {
            Document programDocument = CreateDocument(ProgramCsFile);
            Document programDocumentNoStartup = CreateDocument(ProgramCsFileNoStartup);
            Document programDocumentDifferentStartup = CreateDocument(ProgramCsFileWithDifferentStartup);

            string startupName = await ProjectModifierHelper.GetStartupClassName(programDocument);
            string emptyStartupName = await ProjectModifierHelper.GetStartupClassName(programDocumentNoStartup);
            string notStartupName = await ProjectModifierHelper.GetStartupClassName(programDocumentDifferentStartup);
            string nullStartup = await ProjectModifierHelper.GetStartupClassName(null);

            Assert.Equal("Startup", startupName);
            Assert.Equal("", emptyStartupName);
            Assert.Equal("", nullStartup);
            Assert.Equal("NotStartup", notStartupName);
        }
    }
}
