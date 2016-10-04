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

        [Fact (Skip = "Disabling E2E test")]
        public void TestAreaGenerator()
        {
            var args = new string[]
            {
                "-p",
                _testProjectPath,
                "area",
                "Admin"
            };

            Scaffold(args);
            var generatedFilePath = Path.Combine(_testProjectPath, "ScaffoldingReadMe.txt");
            var baselinePath = Path.Combine("ReadMe", "Readme.txt");

            var foldersToVerify = new string[]
            {
                Path.Combine(_testProjectPath, "Areas", "Admin", "Controllers"),
                Path.Combine(_testProjectPath, "Areas", "Admin", "Data"),
                Path.Combine(_testProjectPath, "Areas", "Admin", "Models"),
                Path.Combine(_testProjectPath, "Areas", "Admin", "Views")
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
