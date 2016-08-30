using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace E2E_Test
{
    [Collection("ScaffoldingE2ECollection")]
    public class AreaScaffolderTests : E2ETestBase
    {
        public AreaScaffolderTests(ScaffoldingE2ETestFixture fixture) : base(fixture)
        {
        }

        [Fact]
        public void TestAreaGenerator()
        {
            var args = new string[]
            {
                "-p",
                testProjectPath,
                "area",
                "Admin"
            };

            Scaffold(args);
            var generatedFilePath = Path.Combine(testProjectPath, "ScaffoldingReadme.txt");
            var baselinePath = Path.Combine("ReadMe", "Readme.txt");

            var foldersToVerify = new string[]
            {
                Path.Combine(testProjectPath, "Areas", "Admin", "Controllers"),
                Path.Combine(testProjectPath, "Areas", "Admin", "Data"),
                Path.Combine(testProjectPath, "Areas", "Admin", "Models"),
                Path.Combine(testProjectPath, "Areas", "Admin", "Views")
            };

            VerifyFileAndContent(generatedFilePath, baselinePath);
            foreach (var folder in foldersToVerify)
            {
                VerifyFoldersCreated(folder);
            }

            _fixture.FoldersToCleanUp.AddRange(foldersToVerify);
            _fixture.FilesToCleanUp.Add(generatedFilePath);
        }
    }
}
