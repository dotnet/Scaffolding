// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.IO;
using System.Runtime.InteropServices;
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

        [Fact]
        public void TestAreaGenerator()
        {
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
