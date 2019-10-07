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
    public class RazorPageScaffolderTests : E2ETestBase
    {

        private static string[] EMPTY_PAGE_ARGS = new string[] { "-c", Configuration, "razorpage", "EmptyPage", "Empty", "--bootstrapVersion", "3" };
        private static string[] PAGE_WITH_DATACONTEXT = new string[] { "-c", Configuration, "razorpage", "CarCreate", "Create", "--model", "Library1.Models.Car", "--dataContext", "WebApplication1.Models.CarContext", "--referenceScriptLibraries", "--bootstrapVersion", "3" };
        private static string[] CRUD_PAGES = new string[] { "-c", Configuration, "razorpage", "--model", "Library1.Models.Car", "--dataContext", "WebApplication1.Models.CarContext", "--referenceScriptLibraries", "--partialView", "--bootstrapVersion", "3" };
        private static string[] PAGE_WITH_DATACONTEXT_IN_DEPENDENCY = new string[] { "-c", Configuration, "razorpage", "CarCreate", "Create", "--model", "Library1.Models.Car", "--dataContext", "DAL.CarContext", "--referenceScriptLibraries", "--bootstrapVersion", "3" };

        public RazorPageScaffolderTests(ITestOutputHelper output)
            :base (output)
        {

        }

        public static IEnumerable<object[]> TestData
        {
            get
            {
                return new[]
                {
                    new object[]    // Empty Razor Page.
                    {
                        new []
                        {
                            Path.Combine("RazorPages", "EmptyPage.txt"),
                            Path.Combine("RazorPages", "EmptyPageCs.txt")
                        },
                        new string[] 
                        {
                            "EmptyPage.cshtml",
                            "EmptyPage.cshtml.cs"
                        },
                        EMPTY_PAGE_ARGS
                    },
                    new object[]    // Razor Page with Model and DataContext
                    {
                        new []
                        {
                            Path.Combine("RazorPages", "CarCreate.txt"),
                            Path.Combine("RazorPages", "CarCreateCs.txt"),
                        },
                        new []
                        {
                            "CarCreate.cshtml",
                            "CarCreate.cshtml.cs"
                        },
                        PAGE_WITH_DATACONTEXT
                    },
                    new object[]    // CRUD Razor pages.
                    {
                        new []
                        {
                            Path.Combine("RazorPages", "Crud", "Create.txt"),
                            Path.Combine("RazorPages", "Crud", "CreateCs.txt"),
                            Path.Combine("RazorPages", "Crud", "Delete.txt"),
                            Path.Combine("RazorPages", "Crud", "DeleteCs.txt"),
                            Path.Combine("RazorPages", "Crud", "Details.txt"),
                            Path.Combine("RazorPages", "Crud", "DetailsCs.txt"),
                            Path.Combine("RazorPages", "Crud", "Edit.txt"),
                            Path.Combine("RazorPages", "Crud", "EditCs.txt"),
                            Path.Combine("RazorPages", "Crud", "Index.txt"),
                            Path.Combine("RazorPages", "Crud", "IndexCs.txt"),
                        },
                        new []
                        {
                            "Create.cshtml",
                            "Create.cshtml.cs",
                            "Delete.cshtml",
                            "Delete.cshtml.cs",
                            "Details.cshtml",
                            "Details.cshtml.cs",
                            "Edit.cshtml",
                            "Edit.cshtml.cs",
                            "Index.cshtml",
                            "Index.cshtml.cs",
                        },
                        CRUD_PAGES
                    }
                };
            }
        }

        [SkippableTheory, MemberData(nameof(TestData))]
        public void TestViewGenerator(string[] baselineFiles, string[] generatedFilePaths, string[] args)
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
                for (int i = 0; i < generatedFilePaths.Length; i++)
                {
                    var generatedFilePath = Path.Combine(TestProjectPath, generatedFilePaths[i]);
                    VerifyFileAndContent(generatedFilePath, baselineFiles[i]);
                }
            }
        }

        [SkippableFact]
        public void TestRazorPagesWithDbContextInDependency()
        {
            string runSkippableTests = Environment.GetEnvironmentVariable("SCAFFOLDING_RunSkippableTests");
            Skip.If(string.IsNullOrEmpty(runSkippableTests));

            using (var fileProvider = new TemporaryFileProvider())
            {
                new MsBuildProjectSetupHelper().SetupProjectsWithDbContextInDependency(fileProvider, Output);
                TestProjectPath = Path.Combine(fileProvider.Root, "Root");
                var invocationArgs = new [] {"-p", Path.Combine(TestProjectPath, "Test.csproj") }
                    .Concat(PAGE_WITH_DATACONTEXT_IN_DEPENDENCY)
                    .ToArray();

                Scaffold(invocationArgs, TestProjectPath);
                VerifyFileAndContent(Path.Combine(TestProjectPath, "CarCreate.cshtml"), Path.Combine("RazorPages", "CarCreate.txt"));
                VerifyFileAndContent(Path.Combine(TestProjectPath, "CarCreate.cshtml.cs"), Path.Combine("RazorPages", "CarCreateCsWithDAL.txt"));
            }
        }
    }
}
