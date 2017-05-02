// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.VisualStudio.Web.CodeGeneration.E2E_Test
{
    public class ViewGeneratorTests : E2ETestBase
    {
        private static string[] EMPTY_VIEW_ARGS = new string[] { codegeneratorToolName, "-p", ".", "view", "EmptyView", "Empty" };
        private static string[] VIEW_WITH_DATACONTEXT = new string[] { codegeneratorToolName, "-p", ".", "view", "CarCreate", "Create", "--model", "Library1.Models.Car", "--dataContext", "WebApplication1.Models.CarContext", "--referenceScriptLibraries" };
        private static string[] VIEW_NO_DATACONTEXT = new string[] { codegeneratorToolName, "-p", ".", "view", "CarDetails", "Details", "--model", "Library1.Models.Car", "--partialView" };

        public ViewGeneratorTests(ITestOutputHelper output) : base(output)
        {

        }

        public static IEnumerable<object[]> TestData
        {
            get
            {
                return new[]
                {
                    new object[] { Path.Combine("Views", "EmptyView.txt"), "EmptyView.cshtml", EMPTY_VIEW_ARGS },
                    // Disable tests that need loading assemblies in insideman
                    new object[] { Path.Combine("Views", "CarCreate.txt"), "CarCreate.cshtml", VIEW_WITH_DATACONTEXT },
                    new object[] { Path.Combine("Views", "CarDetails.txt"),"CarDetails.cshtml", VIEW_NO_DATACONTEXT }
                };
            }
        }

        [Theory, MemberData("TestData")]
        public void TestViewGenerator(string baselineFile, string generatedFilePath, string[] args)
        {
            using (var fileProvider = new TemporaryFileProvider())
            {
                new MsBuildProjectSetupHelper().SetupProjects(fileProvider, Output);
                TestProjectPath = Path.Combine(fileProvider.Root, "Root");
                Scaffold(args, TestProjectPath);
                generatedFilePath = Path.Combine(TestProjectPath, generatedFilePath);
                VerifyFileAndContent(generatedFilePath, baselineFile);
            }
        }
    }
}
