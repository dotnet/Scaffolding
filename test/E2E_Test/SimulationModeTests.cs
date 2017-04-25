// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.VisualStudio.Web.CodeGeneration.E2E_Test
{
    public class SimulationModeTests : E2ETestBase
    {
        public SimulationModeTests(ITestOutputHelper output) : base(output)
        {
        }

        [Fact(Skip="Need new CLI that can run tools on netcoreapp2.0")]
        public void TestControllerWithContext()
        {
            using (var fileProvider = new TemporaryFileProvider())
            {
                new MsBuildProjectSetupHelper().SetupProjects(fileProvider, Output);
                TestProjectPath = Path.Combine(fileProvider.Root, "Root");
                var args = new string[]
                {
                    "aspnet-codegenerator",
                    "-p",
                    TestProjectPath,
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
