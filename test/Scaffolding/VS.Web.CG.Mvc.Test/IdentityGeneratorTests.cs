// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.DotNet.Scaffolding.Shared.Project;
using Microsoft.VisualStudio.Web.CodeGeneration;
using Microsoft.VisualStudio.Web.CodeGenerators.Mvc.Identity;
using Moq;
using Xunit;

namespace Microsoft.VisualStudio.Web.CodeGenerators.Mvc
{
    public class IdentityGeneratorTests
    {
        public static IEnumerable<object[]> TestData
        {
            get
            {
                return new[]
                {
                    new object[] {unModifiedStrings, modifiedStrings, new string[] { "TestDbContext", null, "" }, new string[] { "TestUserClass", null, "" } }
                };
            }
        }

        [Theory]
        [MemberData(nameof(TestData))]
        public void ReplaceIdentityStringsTests(
            List<string> unModifiedStrings,
            List<string> modifiedStrings,
            string[] dbContexts,
            string[] identityUserClassNames)
        {
            foreach (var dbContext in dbContexts)
            {
                foreach (var identityUser in identityUserClassNames)
                {
                    for (int i = 0; i < unModifiedStrings.Count; i++)
                    {
                        string editResult = IdentityGenerator.EditIdentityStrings(unModifiedStrings[i], dbContext, identityUser, DotNet.Scaffolding.Shared.DbProvider.SqlServer, 0);
                        Assert.Contains(editResult, modifiedStrings);
                    }
                }
            }
        }

        private static readonly List<string> unModifiedStrings = new List<string> {
            "builder.Services.AddDbContext<{0}>.options.{0}(connectionString)",
            "builder.Services.AddDefaultIdentity<{0}>.AddEntityFrameworkStores<{0}>}",
            "builder.Services.AddDbContext<{0}>",
            "builder.Services.AddDefaultIdentity<{0}>",
            "builder.Services.AddEntityFrameworkStores<{0}>",
            "options.{0}(connectionString).AddDbContext<{0}>",
            "builder.Configuration.GetConnectionString(\"{0}\")",
            "builder.Configuration.GetConnectionString(\"{0}\").options.{0}(connectionString).AddDefaultIdentity<{0}>",
            null,
            ""
        };

        private static readonly List<string> modifiedStrings = new List<string> {
            "builder.Services.AddDbContext<TestDbContext>.options.UseSqlServer(connectionString)",
            "builder.Services.AddDefaultIdentity<TestUserClass>.AddEntityFrameworkStores<TestDbContext>}",
            "builder.Services.AddDbContext<TestDbContext>",
            "builder.Services.AddDefaultIdentity<TestUserClass>",
            "builder.Services.AddEntityFrameworkStores<TestDbContext>",
            "options.UseSqlServer(connectionString).AddDbContext<TestDbContext>",
            "builder.Configuration.GetConnectionString(\"TestDbContextConnection\")",
            "builder.Configuration.GetConnectionString(\"TestDbContextConnection\").options.UseSqlServer(connectionString).AddDefaultIdentity<TestUserClass>",
            "builder.Services.AddDbContext<>.options.UseSqlServer(connectionString)",
            "builder.Services.AddDefaultIdentity<>.AddEntityFrameworkStores<>}",
            "builder.Services.AddDefaultIdentity<>.AddEntityFrameworkStores<TestDbContext>}",
            "builder.Services.AddDefaultIdentity<TestUserClass>.AddEntityFrameworkStores<>}",
            "builder.Services.AddDbContext<>",
            "builder.Services.AddDefaultIdentity<>",
            "builder.Services.AddEntityFrameworkStores<>",
            "options.UseSqlServer(connectionString).AddDbContext<>",
            "builder.Configuration.GetConnectionString(\"Connection\")",
            "builder.Configuration.GetConnectionString(\"Connection\").options.UseSqlServer(connectionString).AddDefaultIdentity<TestUserClass>",
            "builder.Configuration.GetConnectionString(\"TestDbContextConnection\").options.UseSqlServer(connectionString).AddDefaultIdentity<>",
            "builder.Configuration.GetConnectionString(\"Connection\").options.UseSqlServer(connectionString).AddDefaultIdentity<>",
            "",
                ""
        };
    }
}
