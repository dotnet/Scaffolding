// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using Microsoft.DotNet.Scaffolding.Core.ComponentModel;
using Microsoft.DotNet.Tools.Scaffold.Services;
using Spectre.Console;
using Spectre.Console.Flow;

namespace Microsoft.DotNet.Tools.Scaffold.Flow.Steps;

internal class CategoryDiscovery
{
    private readonly IDotNetToolService _dotnetToolService;
    private readonly DotNetToolInfo? _componentPicked;
    public FlowStepState State { get; private set; }
    public CategoryDiscovery(IDotNetToolService dotnetToolService, DotNetToolInfo? componentPicked)
    {
        _dotnetToolService = dotnetToolService;
        _componentPicked = componentPicked;
    }

    public string? Discover(IFlowContext context)
    {
        var allCommands = context.GetCommandInfos();
        var envVars = context.GetTelemetryEnvironmentVariables();
        if (allCommands is null || allCommands.Count == 0)
        {
            allCommands = AnsiConsole
            .Status()
            .WithSpinner()
            .Start("Discovering scaffolders", statusContext =>
            {
                return _dotnetToolService.GetAllCommandsParallel(envVars: envVars);
            });

            if (allCommands is not null)
            {
                context.Set(FlowContextProperties.CommandInfos, allCommands);
            }
        }

        return Prompt(context);
    }

    private string? Prompt(IFlowContext context)
    {
        var allCommands = context.GetCommandInfos();
        var displayCategories = new List<string>();
        //only get categories from the picked DotNetToolInfo
        displayCategories = (_componentPicked != null ? allCommands?.Where(x => x.Key.Equals(_componentPicked.Command)) : allCommands)
            ?.SelectMany(y => y.Value.DisplayCategories)
            ?.Distinct()
            ?.Order()
            ?.ToList();

        //removes 'All' and adds it back at the end
        displayCategories?.Remove(ScaffolderConstants.DEFAULT_CATEGORY);
        displayCategories?.Add(ScaffolderConstants.DEFAULT_CATEGORY);

        var prompt = new FlowSelectionPrompt<string>()
            .Title("[lightseagreen]Pick a scaffolding category: [/]")
            .Converter(GetCategoryDisplayName)
            .AddChoices(displayCategories, navigation: context.Navigation);

        var result = prompt.Show();
        State = result.State;
        return result.Value;
    }

    //change 'All' to '(Show All)' only for display, value recorded from the prompt is still 'All'
    private string GetCategoryDisplayName(string categoryName)
    {
        if (categoryName.Equals(ScaffolderConstants.DEFAULT_CATEGORY, StringComparison.OrdinalIgnoreCase))
        {
            return "(Show All)";
        }

        return categoryName;
    }
}
