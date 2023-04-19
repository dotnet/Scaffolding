// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.VisualStudio.Web.CodeGeneration.E2E_Test
{
    [Collection("E2E_Tests")]
    public class SimulationModeTests : E2ETestBase
    {
        public SimulationModeTests(ITestOutputHelper output) : base(output)
        {
        }

        [Fact(Skip = "test fail")]
        public void TestControllerWithoutContext()
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
                    "--simulation-mode",
                    "controller",
                    "--controllerName",
                    "CarsController",
                    "--relativeFolderPath",
                    "SimControllers"
                };

                Scaffold(args, TestProjectPath);
                var controllerDir = Path.Combine(TestProjectPath, "SimControllers");
                Console.WriteLine($"ControllerDirectory: {controllerDir}");
                Assert.False(Directory.Exists(controllerDir));
                Assert.False(File.Exists(Path.Combine(controllerDir, "CarsController.cs")));
            }
        }
    }
}
