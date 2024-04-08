// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.CodeAnalysis;
using System.Collections.Generic;
using Microsoft.DotNet.Scaffolding.Shared;
using System.Linq;
using System;
using System.IO;
using Microsoft.DotNet.Scaffolding.Shared.CodeModifier.CodeChange;

namespace Microsoft.VisualStudio.Web.CodeGenerators.Mvc.BlazorIdentity
{
    internal static class BlazorIdentityHelper
    {
        internal const string ApplicationDbContext = nameof(ApplicationDbContext);
        internal const string ApplicationUser = nameof(ApplicationUser);
        internal const string OptionsUseConnectionString = "options.{0}(connectionString)";
        internal const string GetConnectionString = nameof(GetConnectionString);
        internal const string UseSqlite = nameof(UseSqlite);
        internal const string UseSqlServer = nameof(UseSqlServer);
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

        internal static string EditIdentityStrings(string stringToModify, string dbContextClassName, string identityUserClassName, DbProvider databaseProvider)
        {
            if (string.IsNullOrEmpty(stringToModify))
            {
                return string.Empty;
            }

            string modifiedString = stringToModify;
            if (stringToModify.Contains(ApplicationDbContext))
            {
                modifiedString = modifiedString.Replace(ApplicationDbContext, dbContextClassName);
            }
            if (stringToModify.Contains(ApplicationUser))
            {
                modifiedString = modifiedString.Replace(ApplicationUser, identityUserClassName);
            }
            if (stringToModify.Contains(OptionsUseConnectionString))
            {
                modifiedString = modifiedString.Replace("options.{0}",
                    databaseProvider.Equals(DbProvider.SQLite) ? $"options.{UseSqlite}" : $"options.{UseSqlServer}");
            }
            if (stringToModify.Contains(GetConnectionString))
            {
                modifiedString = modifiedString.Replace("GetConnectionString(\"{0}\")", $"GetConnectionString(\"{dbContextClassName}Connection\")");
                modifiedString = modifiedString.Replace("Connection string '{0}'", $"Connection string '{dbContextClassName}Connection'");
            }

            return modifiedString;
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

        internal static CodeSnippet[] ApplyIdentityChanges(CodeSnippet[] filteredChanges, string dbContextClassName, string identityUserClassName, DbProvider databaseProvider)
        {
            foreach (var codeChange in filteredChanges)
            {
                codeChange.LeadingTrivia = codeChange.LeadingTrivia ?? new Formatting();
                codeChange.Block = EditIdentityStrings(codeChange.Block, dbContextClassName, identityUserClassName, databaseProvider);
            }

            return filteredChanges;
        }
    }
}
