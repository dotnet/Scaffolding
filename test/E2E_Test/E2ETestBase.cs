using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Microsoft.DotNet.Cli.Utils;
using Xunit;


namespace E2E_Test
{
    public class E2ETestBase
    {
        protected static string testProjectPath = Path.GetFullPath(@"../../TestApps/WebApplication1");

        protected ScaffoldingE2ETestFixture _fixture;
        protected string _testProjectPath;

        public E2ETestBase(ScaffoldingE2ETestFixture fixture)
        {
            _fixture = fixture;
            Directory.SetCurrentDirectory(_fixture.TestProjectDirectory);
        }

        protected void Scaffold(string[] args)
        {
            new CommandFactory()
                .Create("dotnet-aspnet-codegenerator", args)
                .ForwardStdOut()
                .ForwardStdErr()
                .Execute();
        }

        protected void VerifyFileAndContent(string generatedFilePath, string baselineFile)
        {
            Console.WriteLine($"Checking if file is generated at {generatedFilePath}");
            Assert.True(File.Exists(generatedFilePath));
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                // TODO: This is currently to fix the tests on Non windows machine. 
                // The baseline files need to be converted to Unix line endings
                var expectedContents = File.ReadAllText(Path.Combine(_fixture.BaseLineFilesDirectory, baselineFile));
                var actualContents = File.ReadAllText(generatedFilePath);
                Assert.Equal(expectedContents, actualContents);
            }
            return;
        }

        protected void VerifyFoldersCreated(string folderPath)
        {
            Console.WriteLine($"Verifying folder exists: {folderPath}");
            Assert.True(Directory.Exists(folderPath));
        }
    }
}
