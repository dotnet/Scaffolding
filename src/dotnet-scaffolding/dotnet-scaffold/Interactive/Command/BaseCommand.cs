// Copyright (c) Microsoft Corporation. All rights reserved.
using Microsoft.DotNet.Scaffolding.Internal.Services;
using Microsoft.DotNet.Tools.Scaffold.Interactive.Flow;
using Microsoft.DotNet.Tools.Scaffold.Interactive.Services;
using Spectre.Console.Cli;
using Spectre.Console.Flow;

namespace Microsoft.DotNet.Tools.Scaffold.Interactive.Command;

/// <summary>
/// Provides a base class for scaffold commands, handling flow execution and telemetry.
/// </summary>
/// <typeparam name="TSettings">The type of command settings.</typeparam>
internal abstract class BaseCommand<TSettings> : AsyncCommand<TSettings>
    where TSettings : CommandSettings
{
    /// <summary>
    /// Initializes a new instance of the <see cref="BaseCommand{TSettings}"/> class.
    /// </summary>
    /// <param name="flowProvider">The flow provider for creating flows.</param>
    /// <param name="telemetryService">The telemetry service for event tracking.</param>
    protected BaseCommand(IFlowProvider flowProvider, ITelemetryService telemetryService)
    {
        FlowProvider = flowProvider;
        TelemetryService = telemetryService;
    }

    /// <summary>
    /// Gets the flow provider used to create and manage flows.
    /// </summary>
    protected IFlowProvider FlowProvider { get; }

    /// <summary>
    /// Gets the telemetry service for tracking events.
    /// </summary>
    protected ITelemetryService TelemetryService { get; }

    /// <summary>
    /// Runs a flow asynchronously with the specified steps and settings.
    /// </summary>
    /// <param name="flowSteps">The steps to execute in the flow.</param>
    /// <param name="settings">The command settings.</param>
    /// <param name="remainingArgs">The remaining command-line arguments.</param>
    /// <param name="nonInteractive">Whether to run in non-interactive mode.</param>
    /// <param name="showSelectedOptions">Whether to show selected options in the flow.</param>
    /// <returns>The exit code from the flow, or throws if an exception occurs.</returns>
    protected async ValueTask<int> RunFlowAsync(IEnumerable<IFlowStep> flowSteps, TSettings settings, IRemainingArguments remainingArgs, bool nonInteractive = false, bool showSelectedOptions = true)
    {
        // Prepare properties for the flow context.
        var properties = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase)
        {
            { FlowContextProperties.RemainingArgs, remainingArgs },
            { FlowContextProperties.CommandSettings, settings }
        };

        IFlow? flow = null;
        Exception? exception = null;

        try
        {
            // Create and run the flow.
            flow = FlowProvider.GetFlow(flowSteps, properties, nonInteractive, showSelectedOptions);
            return await flow.RunAsync(CancellationToken.None);
        }
        catch (Exception) {}

        // If an exception was captured, throw it; otherwise, return int.MinValue.
        return exception is not null
            ? throw exception
            : int.MinValue;
    }
}
