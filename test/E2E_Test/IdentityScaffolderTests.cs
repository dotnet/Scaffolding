// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.IO;
using System.Runtime.InteropServices;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.VisualStudio.Web.CodeGeneration.E2E_Test
{
    public class IdentityScaffolderTests : E2ETestBase
    {
        public IdentityScaffolderTests(ITestOutputHelper output) : base(output)
        {
        }

        [Fact]
        public void TestIdentityGenerator()
        {
            using (var fileProvider = new TemporaryFileProvider())
            {
                new MsBuildProjectSetupHelper().SetupProjectsForIdentityScaffolder(fileProvider, Output);
                TestProjectPath = Path.Combine(fileProvider.Root, "Root");
                var args = new string[]
                {
                    "-p",
                    Path.Combine(TestProjectPath, "Test.csproj"),
                    "-c",
                    Configuration,
                    "identity"
                };

                Scaffold(args, TestProjectPath);

                foreach(var file in IdentityGeneratorFilesConfig.Templates)
                {
                    Assert.True(File.Exists(Path.Combine(TestProjectPath, file)), $"Template file does not exist: '{Path.Combine(TestProjectPath, file)}'");
                }

                foreach(var file in IdentityGeneratorFilesConfig.StaticFiles)
                {
                    Assert.True(File.Exists(Path.Combine(TestProjectPath, file)), $"Static file does not exist: '{Path.Combine(TestProjectPath, file)}'");
                }

            }
        }
    }
}
