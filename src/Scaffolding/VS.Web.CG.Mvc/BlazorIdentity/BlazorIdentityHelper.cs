// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis;
using System.Collections.Generic;
using Microsoft.DotNet.Scaffolding.Shared;
using System.Linq;
using System;
using System.IO;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Diagnostics;

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

        internal static IList<SyntaxNode> GetBlazorIdentityGlobalNodes(string builderVarName, BlazorIdentityModel blazorIdentityModel, List<MemberDeclarationSyntax> existingMembers)
        {
            Debugger.Launch();
            var dbProviderString = blazorIdentityModel.DatabaseProvider.Equals(DbProvider.SqlServer) ? "UseSqlServer" : "UseSqlite";
            var globalNodes = new List<SyntaxNode>
            {
                SyntaxFactory.GlobalStatement(SyntaxFactory.ParseStatement(($"\n{builderVarName}.Services.AddCascadingAuthenticationState();\n"))),
                SyntaxFactory.GlobalStatement(SyntaxFactory.ParseStatement(($"\n{builderVarName}.Services.AddScoped<IdentityUserAccessor>();\n"))),
                SyntaxFactory.GlobalStatement(SyntaxFactory.ParseStatement(($"\n{builderVarName}.Services.AddScoped<IdentityRedirectManager>();\n"))),
                SyntaxFactory.GlobalStatement(SyntaxFactory.ParseStatement(($"\n{builderVarName}.Services.AddScoped<AuthenticationStateProvider, IdentityRevalidatingAuthenticationStateProvider>();\n"))),
                SyntaxFactory.GlobalStatement(SyntaxFactory.ParseStatement(
                    ($"\n{builderVarName}.Services.AddAuthentication(options =>\r\n{{\r\n    options.DefaultScheme = IdentityConstants.ApplicationScheme;\r\n    options.DefaultSignInScheme = IdentityConstants.ExternalScheme;\r\n}})\r\n.AddIdentityCookies();\n"))),
                SyntaxFactory.GlobalStatement(SyntaxFactory.ParseStatement($"\nvar connectionString = {builderVarName}.Configuration.GetConnectionString(\"DefaultConnection\") ?? throw new InvalidOperationException(\"Connection string 'DefaultConnection' not found.\");\n")),
                SyntaxFactory.GlobalStatement(SyntaxFactory.ParseStatement($"\n{builderVarName}.Services.AddDbContext<{blazorIdentityModel.DbContextName}>(options => \n    options.{dbProviderString}(connectionString));\n")),
                SyntaxFactory.GlobalStatement(SyntaxFactory.ParseStatement($"\n{builderVarName}.Services.AddIdentityCore<{blazorIdentityModel.UserClassName}>(options => options.SignIn.RequireConfirmedAccount = true)\n    .AddEntityFrameworkStores<{blazorIdentityModel.DbContextName}>()\n    .AddSignInManager()\n    .AddDefaultTokenProviders();\n")),
                SyntaxFactory.GlobalStatement(SyntaxFactory.ParseStatement($"\n{builderVarName}.Services.AddSingleton<IEmailSender<{blazorIdentityModel.UserClassName}>, IdentityNoOpEmailSender>();\n"))
            };

            globalNodes = globalNodes.Except(existingMembers).Distinct().ToList();
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

        /// <summary>
        /// returning full file paths (.tt) for all blazor identity templates
        /// TODO throw exception if nothing found, can't really scaffold is no files were found
        /// </summary>
        /// <returns></returns>
        internal static IDictionary<string, string> GetBlazorIdentityFiles(IFileSystem fileSystem, IEnumerable<string> templateFolders)
        {
            var blazorIdentityTemplateFolder = templateFolders.FirstOrDefault(x => x.Contains("BlazorIdentity", StringComparison.OrdinalIgnoreCase));
            if (!string.IsNullOrEmpty(blazorIdentityTemplateFolder) && fileSystem.DirectoryExists(blazorIdentityTemplateFolder))
            {
                var allFiles = fileSystem.EnumerateFiles(blazorIdentityTemplateFolder, "*.tt", SearchOption.AllDirectories);
                return allFiles.ToDictionary(x => GetFormattedRelativeIdentityFile(x), x => x);
            }

            return null;
        }
    }
}
