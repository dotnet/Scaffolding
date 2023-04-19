// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.VisualStudio.Web.CodeGeneration.E2E_Test
{
    [Collection("E2E_Tests")]
    public class AreaScaffolderTests : E2ETestBase
    {
        public AreaScaffolderTests(ITestOutputHelper output)
            : base(output)
        {
        }

        [SkippableFact]
        public void TestAreaGenerator()
        {
            string runSkippableTests = Environment.GetEnvironmentVariable("SCAFFOLDING_RunSkippableTests");
            Skip.If(string.IsNullOrEmpty(runSkippableTests));

            using (var fileProvider = new TemporaryFileProvider())
            {
                new MsBuildProjectSetupHelper().SetupProjects(fileProvider, Output);
                TestProjectPath = Path.Combine(fileProvider.Root, "Root");
                var args = new string[]
                {
                "-p",
                TestProjectPath,
                "-c",
                Configuration,
                "area",
                "Admin"
                };

                Scaffold(args, TestProjectPath);
                var generatedFilePath = Path.Combine(TestProjectPath, "ScaffoldingReadMe.txt");
                var baselinePath = Path.Combine("ReadMe", "Readme.txt");

                var foldersToVerify = new string[]
                {
                Path.Combine(TestProjectPath, "Areas", "Admin", "Controllers"),
                Path.Combine(TestProjectPath, "Areas", "Admin", "Data"),
                Path.Combine(TestProjectPath, "Areas", "Admin", "Models"),
                Path.Combine(TestProjectPath, "Areas", "Admin", "Views")
                };

                VerifyFileAndContent(generatedFilePath, baselinePath);
                foreach (var folder in foldersToVerify)
                {
                    VerifyFoldersCreated(folder);
                }
            }
        }
    }
}
