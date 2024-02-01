// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis;
using System.Collections.Generic;
using Microsoft.DotNet.Scaffolding.Shared;
using System.Linq;

namespace Microsoft.VisualStudio.Web.CodeGenerators.Mvc.BlazorIdentity
{
    internal static class BlazorIdentityHelper
    {
        internal static string GetFormattedRelativeIdentityFile(string fullFileName)
        {
            string identifier = "BlazorIdentity\\";
            int index = fullFileName.IndexOf(identifier);
            if (index != -1)
            {
                string pathAfterIdentifier = fullFileName.Substring(index + identifier.Length);
                string pathAsNamespaceWithoutExtension = StringUtil.GetFilePathWithoutExtension(pathAfterIdentifier);
                return pathAsNamespaceWithoutExtension;
            }

            return string.Empty;
        }

        internal static IList<SyntaxNode> GetBlazorIdentityGlobalNodes(string builderVarName, string dbContextName, string userClassName)
        {
            var globalNodes = new List<SyntaxNode>
            {
                SyntaxFactory.GlobalStatement(SyntaxFactory.ParseStatement(($"\n{builderVarName}.Services.AddCascadingAuthenticationState();\n"))),
                SyntaxFactory.GlobalStatement(SyntaxFactory.ParseStatement(($"\n{builderVarName}.Services.AddScoped<IdentityUserAccessor>();\n"))),
                SyntaxFactory.GlobalStatement(SyntaxFactory.ParseStatement(($"\n{builderVarName}.Services.AddScoped<IdentityRedirectManager>();\n"))),
                SyntaxFactory.GlobalStatement(SyntaxFactory.ParseStatement(($"\n{builderVarName}.Services.AddScoped<AuthenticationStateProvider, IdentityRevalidatingAuthenticationStateProvider>();\n"))),
                SyntaxFactory.GlobalStatement(SyntaxFactory.ParseStatement(
                    ($"\n{builderVarName}.Services.AddAuthentication(options =>\r\n{{\r\n    options.DefaultScheme = IdentityConstants.ApplicationScheme;\r\n    options.DefaultSignInScheme = IdentityConstants.ExternalScheme;\r\n}})\r\n.AddIdentityCookies();\n"))),
                SyntaxFactory.GlobalStatement(SyntaxFactory.ParseStatement($"\nvar connectionString = {builderVarName}.Configuration.GetConnectionString(\"DefaultConnection\") ?? throw new InvalidOperationException(\"Connection string 'DefaultConnection' not found.\");\n")),
                SyntaxFactory.GlobalStatement(SyntaxFactory.ParseStatement($"\n{builderVarName}.Services.AddDbContext<{dbContextName}>(options => \n    options.UseSqlite(connectionString));\n")),
                SyntaxFactory.GlobalStatement(SyntaxFactory.ParseStatement($"\n{builderVarName}.Services.AddIdentityCore<{userClassName}>(options => options.SignIn.RequireConfirmedAccount = true)\n    .AddEntityFrameworkStores<{dbContextName}>()\n    .AddSignInManager()\n    .AddDefaultTokenProviders();\n")),
                SyntaxFactory.GlobalStatement(SyntaxFactory.ParseStatement($"\n{builderVarName}.Services.AddSingleton<IEmailSender<{userClassName}>, IdentityNoOpEmailSender>();\n"))
            };

            return globalNodes;
        }

        internal static IEnumerable<string> GetGeneralT4Files(IFileSystem fileSystem, IEnumerable<string> templateFolders)
        {
            var generalTemplateFolder = templateFolders.FirstOrDefault(x => x.Contains("General", StringComparison.OrdinalIgnoreCase));
            if (!string.IsNullOrEmpty(generalTemplateFolder) && fileSystem.DirectoryExists(generalTemplateFolder))
            {
                return fileSystem.EnumerateFiles(generalTemplateFolder, "*.tt", SearchOption.AllDirectories);
            }

            return null;
        }
    }
}
