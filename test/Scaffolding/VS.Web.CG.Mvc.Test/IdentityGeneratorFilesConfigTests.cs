// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using Microsoft.VisualStudio.Web.CodeGenerators.Mvc.Identity;
using Xunit;

namespace Microsoft.VisualStudio.Web.CodeGenerators.Mvc
{
    public class IdentityGeneratorFilesConfigTests
    {
        [Fact]
        public void TestExistingWwwRootDataIsNotOverwrittenWhenScaffoldAllFilesSelected()
        {
            IdentityGeneratorTemplateModel model = new IdentityGeneratorTemplateModel()
            {
                UseDefaultUI = false,
                HasExistingNonEmptyWwwRoot = true,
                SupportFileLocation = "\\tmp\\"
            };

            IdentityGeneratorFile[] templateFiles = IdentityGeneratorFilesConfig.GetFilesToGenerate(null, model);

            Assert.DoesNotContain(templateFiles, x => x.SourcePath.IndexOf("wwwroot", StringComparison.OrdinalIgnoreCase) > -1);
        }

        [Theory, MemberData(nameof(TestData))]
        public void TestGetTemplates(string dbContextClass,
            string userClass,
            bool isUsingExistingDbContext,
            bool isGenerateCustomUser,
            Predicate<IdentityGeneratorFile> excludeFilter,
            Predicate<IdentityGeneratorFile> includeFilter)
        {
            var templateModel = new IdentityGeneratorTemplateModel()
            {
                DbContextClass = dbContextClass,
                UserClass = userClass,
                IsUsingExistingDbContext = isUsingExistingDbContext,
                IsGenerateCustomUser = isGenerateCustomUser,
                SupportFileLocation = "Pages/Shared/"
            };

            var templateFiles = IdentityGeneratorFilesConfig.GetFilesToGenerate(null, templateModel);

            if (excludeFilter != null)
            {
                Assert.DoesNotContain(templateFiles, excludeFilter);
            }

            if (includeFilter != null)
            {
                Assert.Contains(templateFiles, includeFilter);
            }
        }

        public static IEnumerable<object[]> TestData
        {
            get
            {
                return new[]
                {
                    new object []
                    {
                        "MyApplicationDbContext",
                        "MyApplicationUser",
                        true,
                        false,
                        new Predicate<IdentityGeneratorFile>(t => t.SourcePath == "ApplicationDbContext.cshtml" || t.SourcePath =="ApplicationUser.cshtml"),
                        null
                    },
                    new object []
                    {
                        "MyApplicationDbContext",
                        "MyApplicationUser",
                        false,
                        true,
                        null,
                        new Predicate<IdentityGeneratorFile>(t => t.SourcePath == "ApplicationDbContext.cshtml" || t.SourcePath =="ApplicationUser.cshtml"),
                    },
                    new object []
                    {
                        "MyApplicationDbContext",
                        "MyApplicationUser",
                        true,
                        true,
                        new Predicate<IdentityGeneratorFile>(t => t.SourcePath == "ApplicationDbContext.cshtml" || t.SourcePath =="ApplicationUser.cshtml"),
                        null
                    },
                    new object []
                    {
                        "MyApplicationDbContext",
                        "MyApplicationUser",
                        false,
                        false,
                        new Predicate<IdentityGeneratorFile>(t => t.SourcePath =="ApplicationUser.cshtml"),
                        new Predicate<IdentityGeneratorFile>(t => t.SourcePath == "ApplicationDbContext.cshtml"),
                    },
                };
            }
        }
    }
}
