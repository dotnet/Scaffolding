using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.VisualStudio.Web.CodeGenerators.Mvc.Identity;
using Xunit;

namespace Microsoft.VisualStudio.Web.CodeGenerators.Mvc
{
    public class IdentityGeneratorFilesConfigTests
    {
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
                IsGenerateCustomUser = isGenerateCustomUser
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
                return new []
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
