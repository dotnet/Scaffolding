// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using Microsoft.DotNet.Scaffolding.Core.ComponentModel;
using Microsoft.DotNet.Tools.Scaffold.Services;
using Spectre.Console;
using Spectre.Console.Flow;

namespace Microsoft.DotNet.Tools.Scaffold.Flow.Steps;

/// <summary>
/// Handles the discovery and selection of a scaffolding command for a given component or category.
/// Presents a prompt to the user to pick a command, and manages the state of the flow step.
/// </summary>
internal class CommandDiscovery
{
    private readonly IDotNetToolService _dotnetToolService;
    private readonly DotNetToolInfo? _componentPicked;

    /// <summary>
    /// Initializes a new instance of the <see cref="CommandDiscovery"/> class.
    /// </summary>
    /// <param name="dotnetToolService">Service for dotnet tool operations.</param>
    /// <param name="componentPicked">The selected component, if any.</param>
    public CommandDiscovery(IDotNetToolService dotnetToolService, DotNetToolInfo? componentPicked)
    {
        _dotnetToolService = dotnetToolService;
        _componentPicked = componentPicked;
    }

    /// <summary>
    /// Gets the state of the flow step after execution.
    /// </summary>
    public FlowStepState State { get; private set; }

    /// <summary>
    /// Discovers available commands and prompts the user to select one.
    /// </summary>
    /// <param name="context">The flow context.</param>
    /// <returns>The selected command as a KeyValuePair, or null if none selected.</returns>
    public KeyValuePair<string, CommandInfo>? Discover(IFlowContext context)
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

    /// <summary>
    /// Prompts the user to select a scaffolding command from the available options.
    /// </summary>
    /// <param name="context">The flow context.</param>
    /// <returns>The selected command as a KeyValuePair, or null if none selected.</returns>
    private KeyValuePair<string, CommandInfo>? Prompt(IFlowContext context)
    {
        var allCommands = context.GetCommandInfos();
        if (_componentPicked != null)
        {
            allCommands = allCommands?.Where(x => x.Key.Equals(_componentPicked.Command)).ToList();
        }

        List<string> scaffoldingCategories = [];
        var scaffoldingCategory = context.GetChosenCategory();
        if (string.IsNullOrEmpty(scaffoldingCategory))
        {
            // Get all categories for non-interactive scenario (since no chosen one was found)
            var possibleScaffoldingCategories = context.GetScaffoldingCategories();
            if (possibleScaffoldingCategories is null || possibleScaffoldingCategories.Count == 0)
            {
                return null;
            }
            else
            {
                scaffoldingCategories.AddRange(possibleScaffoldingCategories);
            }
        }
        else
        {
            scaffoldingCategories.Add(scaffoldingCategory);
        }

        var allCommandsByCategory = allCommands?
            .Where(x => x.Value.DisplayCategories.Intersect(scaffoldingCategories).Any())
            .OrderBy(kvp => kvp.Value.DisplayName)
            .ToList();

        var prompt = new FlowSelectionPrompt<KeyValuePair<string, CommandInfo>>()
            .Title("[lightseagreen]Pick a scaffolding command: [/]")
            .Converter(GetCommandInfoDisplayName)
            .AddChoices(allCommandsByCategory, navigation: context.Navigation);

        var result = prompt.Show();
        State = result.State;
        return result.Value;
    }

    /// <summary>
    /// Gets the display name for a command info entry.
    /// </summary>
    /// <param name="commandInfo">The command info key-value pair.</param>
    /// <returns>The display name for the command.</returns>
    private string GetCommandInfoDisplayName(KeyValuePair<string, CommandInfo> commandInfo)
    {
        return $"{commandInfo.Value.DisplayName} {commandInfo.Key.ToSuggestion(withBrackets: true)}";
    }
}
