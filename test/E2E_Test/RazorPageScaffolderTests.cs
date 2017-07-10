// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.VisualStudio.Web.CodeGeneration.E2E_Test
{
    public class RazorPageScaffolderTests : E2ETestBase
    {

        private static string[] EMPTY_PAGE_ARGS = new string[] { codegeneratorToolName, "-p", ".", "-c", Configuration, "razorpage", "EmptyPage", "Empty" };
        private static string[] PAGE_WITH_DATACONTEXT = new string[] { codegeneratorToolName, "-p", ".", "-c", Configuration, "razorpage", "CarCreate", "Create", "--model", "Library1.Models.Car", "--dataContext", "WebApplication1.Models.CarContext", "--referenceScriptLibraries" };
        private static string[] CRUD_PAGES = new string[] { codegeneratorToolName, "-p", ".", "-c", Configuration, "razorpage", "--model", "Library1.Models.Car", "--dataContext", "WebApplication1.Models.CarContext", "--referenceScriptLibraries", "--partialView" };

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

        [Theory, MemberData(nameof(TestData))]
        public void TestViewGenerator(string[] baselineFiles, string[] generatedFilePaths, string[] args)
        {
            using (var fileProvider = new TemporaryFileProvider())
            {
                new MsBuildProjectSetupHelper().SetupProjects(fileProvider, Output);
                TestProjectPath = Path.Combine(fileProvider.Root, "Root");
                Scaffold(args, TestProjectPath);
                for (int i = 0; i < generatedFilePaths.Length; i++)
                {
                    var generatedFilePath = Path.Combine(TestProjectPath, generatedFilePaths[i]);
                    VerifyFileAndContent(generatedFilePath, baselineFiles[i]);
                }
            }
        }
    }
}