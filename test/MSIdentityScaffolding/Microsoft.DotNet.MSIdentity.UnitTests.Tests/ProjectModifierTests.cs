using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.DotNet.MSIdentity.AuthenticationParameters;
using Microsoft.DotNet.MSIdentity.CodeReaderWriter;
using Microsoft.DotNet.MSIdentity.Tool;
using Xunit;

namespace Microsoft.DotNet.MSIdentity.UnitTests.Tests
{

    public class ProjectModifierTests : DocumentBuilderTestBase
    {
        readonly ProjectModifier projectModifier = new ProjectModifier(new ApplicationParameters(), new ProvisioningToolOptions(), new ConsoleLogger());

        [Theory]
        [InlineData(new object[] { new string[] { "Startup.cs", "File.cs", "Test", "", null},
                                   new string[] { "Startup", "File", "", "", "" } })]
        public void GetClassNameTests(string[] classNames, string[] formattedClassNames)
        {
            for (int i = 0; i < classNames.Length; i++)
            {
                string className = classNames[i];
                string formattedClassName = formattedClassNames[i];
                Assert.Equal(projectModifier.GetClassName(className), formattedClassName);
            }
        }

        [Fact]
        public async Task GetStartupClassNameTests()
        {
            Document programDocument = CreateDocument(ProgramCsFile);
            Document programDocumentNoStartup = CreateDocument(ProgramCsFileNoStartup);
            Document programDocumentDifferentStartup = CreateDocument(ProgramCsFileWithDifferentStartup);

            string startupName = await projectModifier.GetStartupClassName(programDocument);
            string emptyStartupName = await projectModifier.GetStartupClassName(programDocumentNoStartup);
            string notStartupName = await projectModifier.GetStartupClassName(programDocumentDifferentStartup);
            string nullStartup = await projectModifier.GetStartupClassName(null);

            Assert.Equal("Startup", startupName);
            Assert.Equal("", emptyStartupName);
            Assert.Equal("", nullStartup);
            Assert.Equal("NotStartup", notStartupName);
        }
    }
}
