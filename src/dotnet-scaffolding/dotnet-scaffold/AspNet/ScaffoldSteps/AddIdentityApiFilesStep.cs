// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using Microsoft.DotNet.Scaffolding.Core.Scaffolders;
using Microsoft.DotNet.Scaffolding.Core.Steps;
using Microsoft.DotNet.Scaffolding.TextTemplating;
using Microsoft.Extensions.Logging;

namespace Microsoft.DotNet.Tools.Scaffold.AspNet.ScaffoldSteps;

internal class AddIdentityApiFilesStep(ILogger<AddIdentityApiFilesStep> logger) : ScaffoldStep
{
    public required string ProjectPath { get; set; }
    public bool Overwrite { get; set; }
    internal IEnumerable<string>? TemplatePaths { get; set; }

    public override Task<bool> ExecuteAsync(ScaffolderContext context, CancellationToken cancellationToken = default)
    {
        var projectDirectory = Path.GetDirectoryName(ProjectPath);
        if (string.IsNullOrEmpty(projectDirectory))
        {
            logger.LogError("Invalid project path for Identity API scaffolding.");
            return Task.FromResult(false);
        }

        var templates = (TemplatePaths ?? new TemplateFoldersUtilities()
            .GetAllFilesForTargetFramework(["IdentityApi"], ProjectPath))
            .Where(path => path.EndsWith(".cs.txt", StringComparison.OrdinalIgnoreCase) ||
                Path.GetFileName(path).Equals("LICENSE.txt", StringComparison.OrdinalIgnoreCase))
            .ToArray();
        if (!templates.Any(path => Path.GetFileName(path).Equals("IdentityApiEndpoints.cs.txt", StringComparison.OrdinalIgnoreCase)) ||
            !templates.Any(path => Path.GetFileName(path).Equals("LICENSE.txt", StringComparison.OrdinalIgnoreCase)))
        {
            logger.LogError("Identity API source templates or license were not found for '{ProjectPath}'.", ProjectPath);
            return Task.FromResult(false);
        }

        var outputDirectory = Path.Combine(projectDirectory, "IdentityApi");
        Directory.CreateDirectory(outputDirectory);
        foreach (var template in templates)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var outputName = template.EndsWith(".cs.txt", StringComparison.OrdinalIgnoreCase)
                ? Path.GetFileNameWithoutExtension(template)
                : Path.GetFileName(template);
            var outputPath = Path.Combine(outputDirectory, outputName);
            if (!Overwrite && File.Exists(outputPath))
            {
                logger.LogInformation("Preserving existing file '{OutputPath}'.", outputPath);
                continue;
            }

            File.Copy(template, outputPath, overwrite: Overwrite);
            logger.LogInformation("Added '{OutputPath}'.", outputPath);
        }

        return Task.FromResult(true);
    }
}
