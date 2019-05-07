// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.VisualStudio.Web.CodeGeneration.E2E_Test
{
    [Collection("E2E_Tests")]
    public class ViewGeneratorTests : E2ETestBase
    {
        private static string[] EMPTY_VIEW_ARGS = new string[] { "-c", Configuration, "view", "EmptyView", "Empty", "--bootstrapVersion", "3" };
        private static string[] VIEW_WITH_DATACONTEXT = new string[] { "-c", Configuration, "view", "CarCreate", "Create", "--model", "Library1.Models.Car", "--dataContext", "WebApplication1.Models.CarContext", "--referenceScriptLibraries", "--bootstrapVersion", "3" };
        private static string[] VIEW_NO_DATACONTEXT = new string[] { "-c", Configuration, "view", "CarDetails", "Details", "--model", "Library1.Models.Car", "--partialView", "--bootstrapVersion", "3" };

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
                    new object[] { Path.Combine("Views", "CarCreate.txt"), "CarCreate.cshtml", VIEW_WITH_DATACONTEXT },
                    new object[] { Path.Combine("Views", "CarDetails.txt"),"CarDetails.cshtml", VIEW_NO_DATACONTEXT }
                };
            }
        }

        [SkippableTheory, MemberData(nameof(TestData))]
        public void TestViewGenerator(string baselineFile, string generatedFilePath, string[] args)
        {
            string runSkippableTests = Environment.GetEnvironmentVariable("SCAFFOLDING_RunSkippableTests");
            Skip.If(string.IsNullOrEmpty(runSkippableTests));

            using (var fileProvider = new TemporaryFileProvider())
            {
                new MsBuildProjectSetupHelper().SetupProjects(fileProvider, Output);
                TestProjectPath = Path.Combine(fileProvider.Root, "Root");
                var invocationArgs = new [] {"-p", Path.Combine(TestProjectPath, "Test.csproj") }
                    .Concat(args)
                    .ToArray();

                Scaffold(invocationArgs, TestProjectPath);
                generatedFilePath = Path.Combine(TestProjectPath, generatedFilePath);
                VerifyFileAndContent(generatedFilePath, baselineFile);
            }
        }
    }
}
