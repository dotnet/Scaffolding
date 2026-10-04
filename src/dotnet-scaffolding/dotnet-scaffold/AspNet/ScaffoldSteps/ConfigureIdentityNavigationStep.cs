// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using System.Text.RegularExpressions;
using Microsoft.DotNet.Scaffolding.Core.Scaffolders;
using Microsoft.DotNet.Scaffolding.Core.Steps;
using Microsoft.DotNet.Scaffolding.Internal.Services;
using Microsoft.Extensions.Logging;

namespace Microsoft.DotNet.Tools.Scaffold.AspNet.ScaffoldSteps;

internal class ConfigureIdentityNavigationStep(
    ILogger<ConfigureIdentityNavigationStep> logger,
    IFileSystem fileSystem) : ScaffoldStep
{
    public required string ProjectPath { get; set; }
    public bool IsRazorPages { get; set; }

    public override Task<bool> ExecuteAsync(ScaffolderContext context, CancellationToken cancellationToken = default)
    {
        var projectDirectory = Path.GetDirectoryName(ProjectPath);
        if (string.IsNullOrEmpty(projectDirectory))
        {
            logger.LogError("Unable to determine the project directory while configuring Identity navigation.");
            return Task.FromResult(false);
        }

        var layoutPath = Path.Combine(projectDirectory, IsRazorPages ? "Pages" : "Views", "Shared", "_Layout.cshtml");
        if (!fileSystem.FileExists(layoutPath))
        {
            logger.LogWarning($"Add the '_LoginPartial' partial to your layout; '{layoutPath}' does not exist.");
            return Task.FromResult(true);
        }

        var original = fileSystem.ReadAllText(layoutPath);
        var updated = AddLoginPartialReference(original);
        if (updated is null)
        {
            logger.LogWarning($"Add the '_LoginPartial' partial to '{layoutPath}'; no navbar navigation list was found.");
        }
        else if (updated != original)
        {
            fileSystem.WriteAllText(layoutPath, updated);
        }

        return Task.FromResult(true);
    }

    internal static string? AddLoginPartialReference(string content)
    {
        // Ignore Razor/HTML comments without changing offsets into the original layout.
        var markup = Regex.Replace(content, @"@\*[\s\S]*?\*@|<!--[\s\S]*?-->",
            match => new string(' ', match.Length));
        if (Regex.IsMatch(markup, @"<partial\b[^>]*\bname\s*=\s*(['""])_LoginPartial\1|(?:Partial|RenderPartial)(?:Async)?\(\s*""_LoginPartial""",
            RegexOptions.IgnoreCase))
        {
            return content;
        }

        var depth = 0;
        foreach (Match tag in Regex.Matches(markup, @"<\s*(/?)\s*ul\b[^>]*>", RegexOptions.IgnoreCase))
        {
            if (depth == 0)
            {
                var attribute = Regex.Match(tag.Value, @"\bclass\s*=\s*(['""])(.*?)\1", RegexOptions.IgnoreCase | RegexOptions.Singleline);
                if (tag.Groups[1].Length != 0 ||
                    !attribute.Groups[2].Value.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries).Contains("navbar-nav", StringComparer.OrdinalIgnoreCase))
                {
                    continue;
                }
            }

            depth += tag.Groups[1].Length == 0 ? 1 : -1;
            if (depth == 0)
            {
                var lineStart = content.LastIndexOf('\n', tag.Index) + 1;
                var indentation = content[lineStart..tag.Index];
                if (indentation.Any(character => !char.IsWhiteSpace(character)))
                {
                    indentation = string.Empty;
                }
                var newline = content.Contains("\r\n", StringComparison.Ordinal) ? "\r\n" : "\n";
                return content.Insert(tag.Index + tag.Length, $"{newline}{indentation}<partial name=\"_LoginPartial\" />");
            }
        }

        return null;
    }
}
