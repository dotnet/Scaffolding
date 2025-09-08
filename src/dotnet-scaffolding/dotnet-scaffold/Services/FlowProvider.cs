// Copyright (c) Microsoft Corporation. All rights reserved.

using Spectre.Console.Flow;

namespace Microsoft.DotNet.Tools.Scaffold.Services;

/// <summary>
/// Provides functionality to create and manage interactive flows for CLI operations.
/// Implements the <see cref="IFlowProvider"/> interface.
/// </summary>
internal class FlowProvider : IFlowProvider
{
    /// <summary>
    /// Initializes a new instance of the <see cref="FlowProvider"/> class.
    /// </summary>
    public FlowProvider()
    {
    }

    /// <inheritdoc />
    public IFlow? CurrentFlow { get; private set; }

    /// <inheritdoc />
    public IFlow GetFlow(IEnumerable<IFlowStep> steps, Dictionary<string, object> properties, bool nonInteractive, bool showSelectedOptions = true)
    {
        // Create a new FlowRunner with the provided steps and properties, and configure its options.
        var flow = new FlowRunner(steps, properties, nonInteractive)
            .Breadcrumbs()
            .SelectedOptions(showSelectedOptions);

        CurrentFlow = flow;

        return flow;
    }
}
