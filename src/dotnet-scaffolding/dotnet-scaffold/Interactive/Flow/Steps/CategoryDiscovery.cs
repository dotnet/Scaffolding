// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using Microsoft.DotNet.Scaffolding.Core.ComponentModel;
using Microsoft.DotNet.Tools.Scaffold.Services;
using Spectre.Console;
using Spectre.Console.Flow;

namespace Microsoft.DotNet.Tools.Scaffold.Interactive.Flow.Steps;

/// <summary>
/// Handles the discovery and selection of scaffolding categories for a given component or all components.
/// Presents a prompt to the user to pick a category, and manages the state of the flow step.
/// </summary>
internal class CategoryDiscovery
{
    private readonly IDotNetToolService _dotnetToolService;
    private readonly DotNetToolInfo? _componentPicked;
    private readonly IStartUpErrorService _startUpErrorService;
    public FlowStepState State { get; private set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="CategoryDiscovery"/> class.
    /// </summary>
    /// <param name="dotnetToolService">Service for dotnet tool operations.</param>
    /// <param name="componentPicked">The selected component, if any.</param>
    /// <param name="startUpErrorService">Service for Azure CLI startup errors.</param>
    public CategoryDiscovery(IDotNetToolService dotnetToolService, DotNetToolInfo? componentPicked, IStartUpErrorService startUpErrorService)
    {
        _dotnetToolService = dotnetToolService;
        _componentPicked = componentPicked;
        _startUpErrorService = startUpErrorService;
    }

    /// <summary>
    /// Discovers available categories and prompts the user to select one.
    /// </summary>
    /// <param name="context">The flow context.</param>
    /// <returns>The selected category name, or null if none selected.</returns>
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

    /// <summary>
    /// Prompts the user to select a scaffolding category from the available options.
    /// </summary>
    /// <param name="context">The flow context.</param>
    /// <returns>The selected category name, or null if none selected.</returns>
    private string? Prompt(IFlowContext context)
    {
        var allCommands = context.GetCommandInfos();
        var displayCategories = new List<string>();
        // Only get categories from the picked DotNetToolInfo if specified
        displayCategories = (_componentPicked != null ? allCommands?.Where(x => x.Key.Equals(_componentPicked.Command)) : allCommands)
            ?.SelectMany(y => y.Value.DisplayCategories)
            ?.Distinct()
            ?.Order()
            ?.ToList();

        // Removes 'All' and adds it back at the end
        displayCategories?.Remove(ScaffolderConstants.DEFAULT_CATEGORY);
        displayCategories?.Add(ScaffolderConstants.DEFAULT_CATEGORY);

        FlowSelectionPrompt<string> prompt = new FlowSelectionPrompt<string>();

        prompt.Title("[lightseagreen]Pick a scaffolding category: [/]");

        if (DisplayEntraIdError(out string errorMessage))
        {
            prompt.AddMessage(errorMessage);
        }
        prompt.Converter(GetCategoryDisplayName)
            .AddChoices(displayCategories, navigation: context.Navigation);

        var result = prompt.Show();
        State = result.State;
        return result.Value;
    }

    /// <summary>
    /// Converts the category name for display, changing 'All' to '(Show All)'.
    /// </summary>
    /// <param name="categoryName">The category name.</param>
    /// <returns>The display name for the category.</returns>
    private string GetCategoryDisplayName(string categoryName)
    {
        if (categoryName.Equals(ScaffolderConstants.DEFAULT_CATEGORY, StringComparison.OrdinalIgnoreCase))
        {
            return "(Show All)";
        }

        return categoryName;
    }

    private bool DisplayEntraIdError(out string errorMessage)
    {
        // Show Azure CLI error if it exists
        string? failingCommand = _startUpErrorService.GetError();
        if (!string.IsNullOrWhiteSpace(failingCommand))
        {
            errorMessage = $"[red]The \"Entra ID\" category is unavailable due to an authentication or authorization issue. Ensure \"{failingCommand}\" works correctly if you would like to add Entra ID Scaffolding.[/]";
            return true;
        }
        errorMessage = string.Empty;
        return false;
    }
}
