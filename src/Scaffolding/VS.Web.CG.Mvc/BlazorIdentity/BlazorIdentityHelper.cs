// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.CodeAnalysis;
using System.Collections.Generic;
using Microsoft.DotNet.Scaffolding.Shared;
using System.Linq;
using System;
using System.IO;
using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
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

        private static readonly HashSet<string> _staticFileExtensions = [".js"];

        internal static void ConfigureAppRenderMode(CodeFile appChanges, string appContent)
        {
            const string placeholder = "$(BlazorRenderMode)";
            var tags = Regex.Matches(appContent,
                @"<(?:HeadOutlet|Routes)\b(?:[^""'>]|""[^""]*""|'[^']*')*>",
                RegexOptions.CultureInvariant, TimeSpan.FromSeconds(1))
                .Select(tag => new
                {
                    Content = tag.Value,
                    Mode = Regex.Match(tag.Value, @"\s@rendermode\s*=\s*(?:""(?<mode>[^""]+)""|'(?<mode>[^']+)')",
                        RegexOptions.CultureInvariant, TimeSpan.FromSeconds(1)).Groups["mode"]
                })
                .Where(tag => tag.Mode.Success && IsInteractiveRenderMode(tag.Mode.Value))
                .ToArray();
            var renderMode = tags.FirstOrDefault()?.Mode.Value;

            var replacements = appChanges.Replacements.Where(change =>
                !(change.ReplaceSnippet?.Any(snippet => snippet.Contains(placeholder, StringComparison.Ordinal)) ?? false) &&
                (renderMode != null || !change.Block.Contains(placeholder, StringComparison.Ordinal))).ToList();

            if (renderMode is null)
            {
                appChanges.Replacements = replacements.ToArray();
                return;
            }

            foreach (var change in replacements)
            {
                change.Block = change.Block.Replace(placeholder, renderMode.TrimStart('@'), StringComparison.Ordinal);
            }
            foreach (var tag in tags)
            {
                var conditionalMode = tag.Mode.Value == renderMode
                    ? "PageRenderMode"
                    : $"@(HttpContext.AcceptsInteractiveRouting() ? {tag.Mode.Value.TrimStart('@')} : null)";
                replacements.Add(new CodeSnippet
                {
                    ReplaceSnippet = new[] { tag.Content },
                    Block = tag.Content.Remove(tag.Mode.Index, tag.Mode.Length).Insert(tag.Mode.Index, conditionalMode)
                });
            }
            appChanges.Replacements = replacements.ToArray();
        }

        private static bool IsInteractiveRenderMode(string renderMode)
        {
            var expression = SyntaxFactory.ParseExpression(renderMode.TrimStart('@'));
            if (expression.ContainsDiagnostics)
            {
                return false;
            }
            while (expression is ParenthesizedExpressionSyntax parenthesized)
            {
                expression = parenthesized.Expression;
            }
            return expression is IdentifierNameSyntax identifier &&
                identifier.Identifier.ValueText is "InteractiveAuto" or "InteractiveServer" or "InteractiveWebAssembly" ||
                expression is MemberAccessExpressionSyntax member &&
                member.Expression.GetLastToken().ValueText == "RenderMode" &&
                member.Name.Identifier.ValueText is "InteractiveAuto" or "InteractiveServer" or "InteractiveWebAssembly" ||
                expression is ObjectCreationExpressionSyntax creation &&
                creation.Type.GetLastToken().ValueText is
                    "InteractiveAutoRenderMode" or "InteractiveServerRenderMode" or "InteractiveWebAssemblyRenderMode";
        }

        internal static string GetFormattedRelativeIdentityFile(string fullFileName)
        {
            string identifier = "BlazorIdentity" + Path.DirectorySeparatorChar;
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
                modifiedString = modifiedString.Replace("GetConnectionString(\"{0}\")", $"GetConnectionString(\"{dbContextClassName}\")");
                modifiedString = modifiedString.Replace("Connection string '{0}'", $"Connection string '{dbContextClassName}'");
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

        internal static IEnumerable<(string TemplateFolder, string FullPath)> GetBlazorIdentityStaticFiles(IFileSystem fileSystem, IEnumerable<string> templateFolders)
        {
            var blazorIdentityTemplateFolder = templateFolders.FirstOrDefault(x => x.Contains("BlazorIdentity", StringComparison.OrdinalIgnoreCase));
            if (!string.IsNullOrEmpty(blazorIdentityTemplateFolder) && fileSystem.DirectoryExists(blazorIdentityTemplateFolder))
            {
                var allFiles = fileSystem.EnumerateFiles(blazorIdentityTemplateFolder, "*.*", SearchOption.AllDirectories);
                return allFiles
                    .Where(f => _staticFileExtensions.Contains(Path.GetExtension(f)))
                    .Select(f => (blazorIdentityTemplateFolder, f));
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

        internal static string BlazorIdentityReadmeFileName = "Scaffolding-README.md";
        internal static string BlazorIdentityReadmeString =
@"Blazor Identity scaffolding has completed successfully.

For setup and configuration information, see https://go.microsoft.com/fwlink/?linkid=2290075.

If the project had identity support prior to scaffolding, ensure that the following changes are present in Program.cs:
1. Correct DbContextClass is used in the following statements :
    - var connectionString = builder.Configuration.GetConnectionString(DBCONTEXT_CONNECTIONSTRING) ...
    - builder.Services.AddDbContext<DBCONTEXT>(options => ...
    - builder.Services.AddIdentityCore<IDENTITY_USER_CLASS>...
        .AddEntityFrameworkStores<DBCONTEXT>() ...
2. Correct Identity User class is being used, (if using the default 'IdentityUser' or a custom IdentityUser class).
    - builder.Services.AddIdentityCore<IDENTITY_USER_CLASS>...
    - builder.Services.AddSingleton<IEmailSender<IDENTITY_USER_CLASS>...
3. Authorization services and cookie-based antiforgery middleware are enabled:
    - builder.Services.AddAuthorization()
    - app.UseAntiforgery() before the Identity endpoints execute
4. For global interactivity, HeadOutlet and Routes use a conditional render mode:
    - HttpContext.AcceptsInteractiveRouting() ? ORIGINAL_RENDER_MODE : null
    - Single- or double-quoted attributes and qualified RenderMode members are supported.
    - Explicit render-mode instances retain their original options, including prerender.
    - Account pages must remain statically server-rendered.
5. For WebAssembly/Auto, the server's AddRazorComponents() chain includes AddAuthenticationStateSerialization(),
   including when the components builder is retained in a local variable.
   The referenced client registers AddAuthorizationCore(), AddCascadingAuthenticationState(), and
   AddAuthenticationStateDeserialization(). Its imports include Microsoft.AspNetCore.Components.Authorization, and its
   RedirectToLogin component performs a full-page load of the server Account/Login page.
   Routes imports the redirect namespace from its owning project, including clients nested under the server directory.
   The client redirect has an explicit, valid namespace; unevaluated MSBuild expressions are not emitted.
   The client also references Microsoft.AspNetCore.Components.WebAssembly.Authentication, matching
   its WebAssembly package version. For centrally managed versions, add that package before scaffolding.
   Client package prerequisites are checked before any application files are generated or modified.
";

    }
}
