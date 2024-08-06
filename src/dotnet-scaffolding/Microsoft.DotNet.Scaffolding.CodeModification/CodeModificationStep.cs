// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using Microsoft.DotNet.Scaffolding.CodeModification.CodeChange;
using Microsoft.DotNet.Scaffolding.CodeModification.Helpers;
using Microsoft.DotNet.Scaffolding.Core.Scaffolders;
using Microsoft.DotNet.Scaffolding.Core.Steps;
using Microsoft.DotNet.Scaffolding.Roslyn.Services;
using Microsoft.Extensions.Logging;

namespace Microsoft.DotNet.Scaffolding.CodeModification;

/// <summary>
/// other than the required properties, expecting either the 'CodeModifierConfigPath'
/// or the 'CodeModifierConfigJsonText' strings to not be null. Will prioritize
/// 'CodeModifierConfigJsonText' property.
/// </summary>
public class CodeModificationStep : ScaffoldStep
{
    public string? CodeModifierConfigPath { get; set; }
    public string? CodeModifierConfigJsonText { get; set; }
    public required IList<string> CodeChangeOptions { get; set; }
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
        CodeModifierConfig? codeModifierConfig = null;
        if (!string.IsNullOrEmpty(CodeModifierConfigJsonText))
        {
            codeModifierConfig = CodeModifierConfigHelper.GetCodeModifierConfigFromJson(CodeModifierConfigJsonText);
        }
        else if(!string.IsNullOrEmpty(CodeModifierConfigPath))
        {
            codeModifierConfig = CodeModifierConfigHelper.GetCodeModifierConfig(CodeModifierConfigPath);
        }
        else
        {
            _logger.LogError($"No {nameof(CodeModifierConfig)} provided. Provide a valid value for either '{nameof(CodeModifierConfigJsonText)}' or '{nameof(CodeModifierConfigPath)}' variable");
            return false;
        }
        
        if (codeModifierConfig is null)
        {
            _logger.LogError($"Unable to parse the {nameof(CodeModifierConfig)} provided. Check the {nameof(CodeModifierConfig)} definition.");
            //log a more specific error message.
            var errorMessage = string.IsNullOrEmpty(CodeModifierConfigJsonText) ?
                $"Invalid {nameof(CodeModifierConfigJsonText)} provided" : $"Invalid config/path provided at {CodeModifierConfigPath}";
            _logger.LogError(errorMessage);
            return false;
        }

        ICodeService codeService = new CodeService(_logger, ProjectPath);
        //replace all "variables" provided in 'CodeModifierProperties' in the 'CodeModifierConfig'
        EditCodeModifierConfig(codeModifierConfig);
        var projectModifier = new ProjectModifier(
            ProjectPath,
            codeService,
            _logger,
            codeModifierConfig,
            CodeChangeOptions);

        string projectName = Path.GetFileNameWithoutExtension(ProjectPath);
        _logger.LogInformation($"Updating project '{projectName}'");
        var projectModificationResult = await projectModifier.RunAsync();
        if (projectModificationResult)
        {
            _logger.LogInformation("Done");
        }
        else
        {
            _logger.LogInformation("Failed");
        }

        return projectModificationResult;
    }

    private void EditCodeModifierConfig(CodeModifierConfig codeModifierConfig)
    {
        if (codeModifierConfig.Files is null)
        {
            return;
        }

        var methods = codeModifierConfig.Files.SelectMany(x => x.Methods?.Values ?? Enumerable.Empty<Method>());
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
