// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using Microsoft.DotNet.Scaffolding.Roslyn.Services;
using Microsoft.DotNet.Scaffolding.Core.Scaffolders;
using Microsoft.DotNet.Scaffolding.Core.Steps;
using Microsoft.Extensions.Logging;

namespace Microsoft.DotNet.Scaffolding.CodeModification;

public class CodeModificationStep : ScaffoldStep
{
    public required CodeModifierConfig CodeModifierConfig { get; set; }
    public required CodeChangeOptions CodeChangeOptions { get; set; }
    //.csproj path for the .NET project
    public required string ProjectPath { get; set; }
    //properties to be injected into the CodeModifierConfig.CodeFile.Method.CodeSnippet's Blocks/Parents/CheckBlock
    public required IDictionary<string, string> CodeModifierProperties { get; set; }
    private readonly ILogger _logger;

    public CodeModificationStep(ILogger<CodeModificationStep> logger)
    {
        _logger = logger;
    }

    public override async Task<bool> ExecuteAsync(ScaffolderContext context, CancellationToken cancellationToken = default)
    {
        ICodeService codeService = new CodeService(_logger, ProjectPath);
        //replace all "variables" provided in 'CodeModifierProperties' in the 'CodeModifierConfig'
        EditCodeModifierConfig();
        var projectModifier = new ProjectModifier(
            ProjectPath,
            codeService,
            _logger,
            CodeModifierConfig,
            CodeChangeOptions);

        string projectName = Path.GetFileName(ProjectPath);
        _logger.LogInformation($"Updating project '{projectName}'");
        return await projectModifier.RunAsync();
    }

    private void EditCodeModifierConfig()
    {
        if (CodeModifierConfig.Files is null)
        {
            return;
        }

        var methods = CodeModifierConfig.Files.SelectMany(x => x.Methods?.Values ?? Enumerable.Empty<Method>());
        var codeSnippets = methods.SelectMany(x => x.CodeChanges ?? Enumerable.Empty<CodeSnippet>());
        foreach (var codeSnippet in codeSnippets)
        {
            codeSnippet.CheckBlock = ReplaceString(codeSnippet.CheckBlock);
            codeSnippet.Parent = ReplaceString(codeSnippet.Parent);
            codeSnippet.Block = ReplaceString(codeSnippet.Block) ?? string.Empty;
        }
    }

    private string? ReplaceString(string? input)
    {
        if (!string.IsNullOrEmpty(input))
        {
            foreach (var kvp in CodeModifierProperties)
            {
                input = input.Replace(kvp.Key, kvp.Value);
            }
        }

        return input;
    }
}
