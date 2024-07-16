// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using Microsoft.DotNet.Scaffolding.Helpers.Roslyn;
using Microsoft.DotNet.Scaffolding.Helpers.Services;

namespace Microsoft.DotNet.Scaffolding.Helpers.Steps;

internal class CodeChangeStep : ScaffoldStep
{
    public required CodeModifierConfig CodeModifierConfig { get; set; }
    //.csproj path for the .NET project
    public required string ProjectPath { get; init; }
    public required ILogger Logger { get; init; }
    //properties to be injected into the CodeModifierConfig.CodeFile.Method.CodeSnippet's Blocks/Parents/CheckBlock
    public required IDictionary<string, string> CodeModifierProperties { get; init; }
    public ICodeService? CodeService { get; set; }

    public override async Task<bool> ExecuteAsync()
    {
        new MsBuildInitializer(Logger).Initialize();
        if (CodeService is null)
        {
            var workspaceSettings = new WorkspaceSettings()
            {
                InputPath = ProjectPath
            };

            var appSettings = new AppSettings();
            appSettings.AddSettings("workspace", workspaceSettings);
            CodeService = new CodeService(appSettings, Logger);
        }

        //replace all "variables" provided in 'CodeModifierProperties' in the 'CodeModifierConfig'
        EditCodeModifierConfig();
        var projectModifier = new ProjectModifier(
            ProjectPath,
            CodeService,
            Logger,
            CodeModifierConfig);

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
        if (string.IsNullOrEmpty(input))
        {
            return null;
        }

        foreach (var kvp in CodeModifierProperties)
        {
            input = input.Replace(kvp.Key, kvp.Value);
        }

        return input;
    }
}
