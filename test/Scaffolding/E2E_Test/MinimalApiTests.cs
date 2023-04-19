// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.VisualStudio.Web.CodeGeneration.E2E_Test
{
    [Collection("E2E_Tests")]
    public class MinimalApiTests : E2ETestBase
    {
        public MinimalApiTests(ITestOutputHelper output) : base(output)
        {
        }

        [SkippableFact]
        public void TestMinimalApiGenerator()
        {
            string runSkippableTests = Environment.GetEnvironmentVariable("SCAFFOLDING_RunSkippableTests");
            Skip.If(string.IsNullOrEmpty(runSkippableTests));

            using (var fileProvider = new TemporaryFileProvider())
            {
                new MsBuildProjectSetupHelper().SetupProjectsForMinimalApiScaffolder(fileProvider, Output);
                TestProjectPath = Path.Combine(fileProvider.Root, "Root");
                var args = new string[]
                {
                    "-p",
                    Path.Combine(TestProjectPath, "Test.csproj"),
                    "-c",
                    Configuration,
                    "minimalapi",
                    "-m",
                    "Car",
                    "-e",
                    "EndpointsClass"
                };

                Scaffold(args, TestProjectPath);
                var endpointsFIle = Path.Combine(TestProjectPath, "EndpointsClass.cs");
                Assert.True(File.Exists(endpointsFIle));
                Assert.True(!string.IsNullOrEmpty(File.ReadAllText(endpointsFIle)));
            }
        }
    }
}
