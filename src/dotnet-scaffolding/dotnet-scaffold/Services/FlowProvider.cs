// Copyright (c) Microsoft Corporation. All rights reserved.

using Spectre.Console.Flow;

namespace Microsoft.DotNet.Tools.Scaffold.Services;

internal class FlowProvider : IFlowProvider
{
    public FlowProvider()
    {
    }

    /// <inheritdoc />
    public IFlow? CurrentFlow { get; private set; }

    /// <inheritdoc />
    public IFlow GetFlow(IEnumerable<IFlowStep> steps, Dictionary<string, object> properties, bool nonInteractive, bool showSelectedOptions = true)
    {
        var flow = new FlowRunner(steps, properties, nonInteractive)
            .Breadcrumbs()
            .SelectedOptions(showSelectedOptions);

        CurrentFlow = flow;

        return flow;
    }
}
