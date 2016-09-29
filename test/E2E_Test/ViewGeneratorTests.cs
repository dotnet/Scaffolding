// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace E2E_Test
{
    [Collection("ScaffoldingE2ECollection")]
    public class ViewGeneratorTests : E2ETestBase
    {
        private static string[] EMPTY_VIEW_ARGS = new string[] { "-p", ".", "view", "EmptyView", "Empty" };
        private static string[] VIEW_WITH_DATACONTEXT = new string[] { "-p", ".", "view", "CarCreate", "Create", "--model", "WebApplication1.Models.Car", "--dataContext", "WebApplication1.Models.CarContext", "--referenceScriptLibraries" };
        private static string[] VIEW_NO_DATACONTEXT = new string[] { "-p", ".", "view", "CarDetails", "Details", "--model", "WebApplication1.Models.Car", "--dataContext", "WebApplication1.Models.CarContext", "--partialView" };

        public ViewGeneratorTests(ScaffoldingE2ETestFixture fixture) : base(fixture)
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

        [Theory(Skip = "Disabling E2E test"), MemberData("TestData")]
        public void TestViewGenerator(string baselineFile, string generatedFilePath, string[] args)
        {
            Scaffold(args);
            generatedFilePath = Path.Combine(_testProjectPath, generatedFilePath);
            VerifyFileAndContent(generatedFilePath, baselineFile);
            _fixture.FilesToCleanUp.Add(generatedFilePath);
        }
    }
}