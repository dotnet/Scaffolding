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
            Predicate<KeyValuePair<string,string>> excludeFilter,
            Predicate<KeyValuePair<string, string>> includeFilter)
        {
            var templateModel = new IdentityGeneratorTemplateModel()
            {
                DbContextClass = dbContextClass,
                UserClass = userClass,
                IsUsingExistingDbContext = isUsingExistingDbContext,
                IsGenerateCustomUser = isGenerateCustomUser
            };

            var templateFiles = IdentityGeneratorFilesConfig.GetTemplateFiles(templateModel);

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
                        new Predicate<KeyValuePair<string, string>>(t => t.Key == "ApplicationDbContext.cshtml" || t.Key =="ApplicationUser.cshtml"),
                        null
                    },
                    new object []
                    {
                        "MyApplicationDbContext",
                        "MyApplicationUser",
                        false,
                        true,
                        null,
                        new Predicate<KeyValuePair<string, string>>(t => t.Key == "ApplicationDbContext.cshtml" || t.Key =="ApplicationUser.cshtml"),
                    },
                    new object []
                    {
                        "MyApplicationDbContext",
                        "MyApplicationUser",
                        true,
                        true,
                        new Predicate<KeyValuePair<string, string>>(t => t.Key == "ApplicationDbContext.cshtml" || t.Key =="ApplicationUser.cshtml"),
                        null
                    },
                    new object []
                    {
                        "MyApplicationDbContext",
                        "MyApplicationUser",
                        false,
                        false,
                        new Predicate<KeyValuePair<string, string>>(t => t.Key =="ApplicationUser.cshtml"),
                        new Predicate<KeyValuePair<string, string>>(t => t.Key == "ApplicationDbContext.cshtml"),
                    },
                };
            }
        }
    }
}
