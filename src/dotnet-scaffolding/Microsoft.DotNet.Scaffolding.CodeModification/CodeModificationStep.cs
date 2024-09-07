// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
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
    public IDictionary<string, string> CodeModifierProperties { get; } 
    private readonly ILogger _logger;

    public CodeModificationStep(ILogger<CodeModificationStep> logger)
    {
        _logger = logger;
        CodeModifierProperties = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        CodeChangeOptions ??= [];
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
        if (await ProjectModifierHelper.IsUsingTopLevelStatementsAsync(codeService))
        {
            CodeChangeOptions.Add(Constants.UseTopLevelStatements);
        }

        //replace all "variables" provided in 'CodeModifierProperties' in the 'CodeModifierConfig'
        codeModifierConfig.EditCodeModifierConfig(CodeModifierProperties);
        var projectModifier = new ProjectModifier(
            ProjectPath,
            codeService,
            _logger,
            codeModifierConfig,
            CodeChangeOptions);

        string projectName = Path.GetFileNameWithoutExtension(ProjectPath);
        _logger.LogInformation($"Updating project '{projectName}'...");
        var projectModificationResult = await projectModifier.RunAsync();
        if (projectModificationResult)
        {
            _logger.LogInformation("Done");
        }
        else
        {
            _logger.LogError("Failed");
        }

        return projectModificationResult;
    }
}
