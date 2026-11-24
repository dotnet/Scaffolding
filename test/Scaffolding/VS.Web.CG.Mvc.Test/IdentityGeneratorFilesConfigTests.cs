// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.VisualStudio.Web.CodeGenerators.Mvc.Identity;
using Xunit;

namespace Microsoft.VisualStudio.Web.CodeGenerators.Mvc
{
    public class IdentityGeneratorFilesConfigTests
    {
        [Theory]
        [InlineData("Bootstrap4")]
        [InlineData("Bootstrap5")]
        public void IdentityLayoutsUseConfiguredAssetPathPrefix(string bootstrapVersion)
        {
            DirectoryInfo directory = new DirectoryInfo(AppContext.BaseDirectory);
            while (directory != null && !File.Exists(Path.Combine(directory.FullName, "All.sln")))
            {
                directory = directory.Parent;
            }

            Assert.NotNull(directory);
            string templatePath = Path.Combine(
                directory.FullName,
                "src", "Scaffolding", "VS.Web.CG.Mvc",
                "Templates", "Identity", bootstrapVersion,
                "Pages", "_Layout.cshtml");
            string template = File.ReadAllText(templatePath);

            Assert.Contains("Model.GetAssetPath", template);
            Assert.DoesNotContain("~/Identity/", template);

            string validationPartialPath = Path.Combine(
                directory.FullName,
                "src", "Scaffolding", "VS.Web.CG.Mvc",
                "Templates", "Identity", bootstrapVersion,
                "Pages", "_ValidationScriptsPartial.cshtml");
            string validationPartial = File.ReadAllText(validationPartialPath);

            Assert.Contains("Model.GetAssetPath(\"/lib/jquery-validation/dist/jquery.validate.min.js\")", validationPartial);
            Assert.Contains("Model.GetAssetPath(\"/lib/jquery-validation-unobtrusive/dist/jquery.validate.unobtrusive.min.js\")", validationPartial);
            Assert.DoesNotContain("src=\"~/lib/jquery-validation/", validationPartial);
        }

        [Theory]
        [InlineData("Bootstrap4", false)]
        [InlineData("Bootstrap4", true)]
        [InlineData("Default", false)]
        [InlineData("Default", true)]
        public void IdentityAssetsAreScaffoldedToTheSelectedLocalPath(
            string contentVersion,
            bool hasExistingNonEmptyWwwRoot)
        {
            IdentityGeneratorTemplateModel2 model = new IdentityGeneratorTemplateModel2()
            {
                ContentVersion = contentVersion,
                UseDefaultUI = false,
                HasExistingNonEmptyWwwRoot = hasExistingNonEmptyWwwRoot,
                SupportFileLocation = "Pages/Shared"
            };

            IdentityGeneratorFile[] files = IdentityGeneratorFilesConfig.GetFilesToGenerate(null, model);
            IdentityGeneratorFile siteCss = Assert.Single(files, file => file.Name == "Site.css");
            IdentityGeneratorFile bootstrapBundle = Assert.Single(files, file => file.Name == "Bootstrap.bundle.js");
            IdentityGeneratorFile minifiedBootstrapBundle = Assert.Single(files, file => file.Name == "Bootstrap.bundle.min.js");
            IdentityGeneratorFile jqueryValidate = Assert.Single(files, file => file.Name == "Jquery.validate.min.js");
            IdentityGeneratorFile jqueryValidateUnobtrusive = Assert.Single(files, file => file.Name == "Jquery.validate.unobtrusive.min.js");
            string expectedAssetRoot = hasExistingNonEmptyWwwRoot
                ? Path.Combine("wwwroot", "Identity")
                : "wwwroot";

            Assert.Equal(Path.Combine(expectedAssetRoot, "css", "site.css"), siteCss.OutputPath.Replace('/', Path.DirectorySeparatorChar));
            Assert.Equal(Path.Combine(expectedAssetRoot, "lib", "bootstrap", "dist", "js", "bootstrap.bundle.js"), bootstrapBundle.OutputPath.Replace('/', Path.DirectorySeparatorChar));
            Assert.Equal(Path.Combine(expectedAssetRoot, "lib", "bootstrap", "dist", "js", "bootstrap.bundle.min.js"), minifiedBootstrapBundle.OutputPath.Replace('/', Path.DirectorySeparatorChar));
            Assert.Equal(Path.Combine(expectedAssetRoot, "lib", "jquery-validation", "dist", "jquery.validate.min.js"), jqueryValidate.OutputPath.Replace('/', Path.DirectorySeparatorChar));
            Assert.Equal(Path.Combine(expectedAssetRoot, "lib", "jquery-validation-unobtrusive", "dist", "jquery.validate.unobtrusive.min.js"), jqueryValidateUnobtrusive.OutputPath.Replace('/', Path.DirectorySeparatorChar));
            Assert.Equal(
                hasExistingNonEmptyWwwRoot ? "~/Identity/css/site.css" : "~/css/site.css",
                model.GetAssetPath("/css/site.css"));
            Assert.Equal(
                hasExistingNonEmptyWwwRoot
                    ? "~/Identity/lib/jquery-validation/dist/jquery.validate.min.js"
                    : "~/lib/jquery-validation/dist/jquery.validate.min.js",
                model.GetAssetPath("/lib/jquery-validation/dist/jquery.validate.min.js"));
            Assert.Equal(
                hasExistingNonEmptyWwwRoot
                    ? "~/Identity/lib/jquery-validation-unobtrusive/dist/jquery.validate.unobtrusive.min.js"
                    : "~/lib/jquery-validation-unobtrusive/dist/jquery.validate.unobtrusive.min.js",
                model.GetAssetPath("/lib/jquery-validation-unobtrusive/dist/jquery.validate.unobtrusive.min.js"));
            if (hasExistingNonEmptyWwwRoot)
            {
                Assert.Equal(OverWriteCondition.Never, siteCss.ShouldOverWrite);
                Assert.Equal(OverWriteCondition.Never, bootstrapBundle.ShouldOverWrite);
                Assert.Equal(OverWriteCondition.Never, minifiedBootstrapBundle.ShouldOverWrite);
                Assert.Equal(OverWriteCondition.Never, jqueryValidate.ShouldOverWrite);
                Assert.Equal(OverWriteCondition.Never, jqueryValidateUnobtrusive.ShouldOverWrite);
            }
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void IdentityValidationScriptsPartialIsRenderedAsATemplate(bool generateLayout)
        {
            IdentityGeneratorTemplateModel model = new IdentityGeneratorTemplateModel()
            {
                GenerateLayout = generateLayout,
                Layout = generateLayout ? "Pages/Shared/_Layout.cshtml" : null,
                SupportFileLocation = "Pages/Shared"
            };

            IdentityGeneratorFile[] files = IdentityGeneratorFilesConfig.GetFilesToGenerate(null, model);
            string expectedOutputPath = generateLayout
                ? Path.Combine("Pages", "Shared", "_ValidationScriptsPartial.cshtml")
                : Path.Combine("Areas", "Identity", "Pages", "_ValidationScriptsPartial.cshtml");
            IdentityGeneratorFile validationPartial = Assert.Single(
                files,
                file => file.Name == "_ValidationScriptsPartial"
                    && file.OutputPath.Replace('\\', '/') == expectedOutputPath.Replace('\\', '/'));

            Assert.True(validationPartial.IsTemplate);
        }

        [Fact]
        public void TestExistingWwwRootDataIsNotOverwrittenWhenScaffoldAllFilesSelected()
        {
            IdentityGeneratorTemplateModel model = new IdentityGeneratorTemplateModel()
            {
                UseDefaultUI = false,
                HasExistingNonEmptyWwwRoot = true,
                SupportFileLocation = "Pages/Shared"
            };

            IdentityGeneratorFile[] templateFiles = IdentityGeneratorFilesConfig.GetFilesToGenerate(null, model);

            Assert.Contains(
                templateFiles,
                file => file.OutputPath == Path.Combine("wwwroot", "Identity", "css", "site.css")
                    && file.ShouldOverWrite == OverWriteCondition.Never);
            Assert.DoesNotContain(
                templateFiles,
                file => file.OutputPath.Replace('\\', '/').StartsWith("wwwroot/", StringComparison.OrdinalIgnoreCase)
                    && !file.OutputPath.Replace('\\', '/').StartsWith("wwwroot/Identity/", StringComparison.OrdinalIgnoreCase));
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
