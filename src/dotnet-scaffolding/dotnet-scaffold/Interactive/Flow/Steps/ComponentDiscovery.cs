// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using Microsoft.DotNet.Scaffolding.Core.ComponentModel;
using Microsoft.DotNet.Tools.Scaffold.Services;
using Spectre.Console.Flow;

namespace Microsoft.DotNet.Tools.Scaffold.Interactive.Flow.Steps;

/// <summary>
/// Handles the discovery and selection of a scaffolding component (dotnet tool).
/// Presents a prompt to the user to pick a component from the available list.
/// </summary>
internal class ComponentDiscovery
{
    private readonly IDotNetToolService _dotnetToolService;

    /// <summary>
    /// Initializes a new instance of the <see cref="ComponentDiscovery"/> class.
    /// </summary>
    /// <param name="dotNetToolService">Service for dotnet tool operations.</param>
    public ComponentDiscovery(IDotNetToolService dotNetToolService)
    {
        _dotnetToolService =  dotNetToolService;
    }
    /// <summary>
    /// Gets the state of the flow step after execution.
    /// </summary>
    public FlowStepState State { get; private set; }

    /// <summary>
    /// Discovers available components and prompts the user to select one.
    /// </summary>
    /// <param name="context">The flow context.</param>
    /// <returns>The selected <see cref="DotNetToolInfo"/>, or null if none selected.</returns>
    public DotNetToolInfo? Discover(IFlowContext context)
    {
        var dotnetTools = _dotnetToolService.GetDotNetTools();
        return Prompt(context, "Pick a scaffolding component ('dotnet tool')", dotnetTools);
    }

    /// <summary>
    /// Prompts the user to select a component from the available list.
    /// </summary>
    /// <param name="context">The flow context.</param>
    /// <param name="title">Prompt title.</param>
    /// <param name="components">List of available components.</param>
    /// <returns>The selected <see cref="DotNetToolInfo"/>, or null if none selected.</returns>
    private DotNetToolInfo? Prompt(IFlowContext context, string title, IList<DotNetToolInfo> components)
    {
        if (components.Count == 0)
        {
            return null;
        }

        if (components.Count == 1)
        {
            return components[0];
        }

        var prompt = new FlowSelectionPrompt<DotNetToolInfo>()
            .Title(title)
            .Converter(GetDotNetToolInfoDisplayString)
            .AddChoices(components, navigation: context.Navigation);

        var result = prompt.Show();
        State = result.State;
        return result.Value;
    }

    /// <summary>
    /// Gets the display string for a <see cref="DotNetToolInfo"/>.
    /// </summary>
    /// <param name="dotnetToolInfo">The dotnet tool info.</param>
    /// <returns>The display string.</returns>
    internal string GetDotNetToolInfoDisplayString(DotNetToolInfo dotnetToolInfo)
    {
        return dotnetToolInfo.ToDisplayString();
    }
}
