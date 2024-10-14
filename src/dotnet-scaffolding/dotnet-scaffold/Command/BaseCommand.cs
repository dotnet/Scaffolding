// Copyright (c) Microsoft Corporation. All rights reserved.
using Microsoft.DotNet.Scaffolding.Internal.Services;
using Microsoft.DotNet.Tools.Scaffold.Flow;
using Microsoft.DotNet.Tools.Scaffold.Services;
using Spectre.Console.Cli;
using Spectre.Console.Flow;

namespace Microsoft.DotNet.Tools.Scaffold.Command;

internal abstract class BaseCommand<TSettings> : AsyncCommand<TSettings>
    where TSettings : CommandSettings
{
    protected BaseCommand(IFlowProvider flowProvider, ITelemetryService telemetryService)
    {
        FlowProvider = flowProvider;
        TelemetryService = telemetryService;
    }

    protected IFlowProvider FlowProvider { get; }

    protected ITelemetryService TelemetryService { get; }

    protected async ValueTask<int> RunFlowAsync(IEnumerable<IFlowStep> flowSteps, TSettings settings, IRemainingArguments remainingArgs, bool nonInteractive = false, bool showSelectedOptions = true)
    {
        var properties = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase)
        {
            { FlowContextProperties.RemainingArgs, remainingArgs },
            { FlowContextProperties.CommandSettings, settings }
        };

        IFlow? flow = null;
        Exception? exception = null;

        try
        {
            flow = FlowProvider.GetFlow(flowSteps, properties, nonInteractive, showSelectedOptions);
            return await flow.RunAsync(CancellationToken.None);
        }

        catch (Exception) {}

        return exception is not null
            ? throw exception
            : int.MinValue;
    }
}
