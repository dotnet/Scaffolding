// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using Microsoft.DotNet.Scaffolding.Core.ComponentModel;
using Microsoft.DotNet.Scaffolding.Internal.CliHelpers;
using Microsoft.DotNet.Scaffolding.Internal.Services;
using Microsoft.DotNet.Scaffolding.Internal.Telemetry;
using Microsoft.DotNet.Tools.Scaffold.Telemetry;
using Spectre.Console;
using Spectre.Console.Flow;

namespace Microsoft.DotNet.Tools.Scaffold.Flow.Steps
{
    /// <summary>
    /// IFlowStep where we gather all the parameter values, and then execute the chosen/given component (dotnet tool).
    /// Prints out stdout/stderr and tracks telemetry for the execution.
    /// </summary>
    internal class CommandExecuteFlowStep : IFlowStep
    {
        private readonly ITelemetryService _telemetryService;

        /// <summary>
        /// Initializes a new instance of the <see cref="CommandExecuteFlowStep"/> class.
        /// </summary>
        /// <param name="telemetryService">The telemetry service to use for tracking events.</param>
        public CommandExecuteFlowStep(ITelemetryService telemetryService)
        {
            _telemetryService = telemetryService;
        }

        /// <inheritdoc/>
        public string Id => nameof(CommandExecuteFlowStep);
        /// <inheritdoc/>
        public string DisplayName => "Command Execute";

        /// <inheritdoc/>
        public ValueTask ResetAsync(IFlowContext context, CancellationToken cancellationToken)
        {
            return ValueTask.CompletedTask;
        }

        /// <inheritdoc/>
        public ValueTask<FlowStepResult> RunAsync(IFlowContext context, CancellationToken cancellationToken)
        {
            return new ValueTask<FlowStepResult>(FlowStepResult.Success);
        }

        /// <inheritdoc/>
        public ValueTask<FlowStepResult> ValidateUserInputAsync(IFlowContext context, CancellationToken cancellationToken)
        {
            // Need all 3 things, throw if not found
            var dotnetToolInfo = context.GetComponentObj();
            var commandObj = context.GetCommandObj();
            if (dotnetToolInfo is null || commandObj is null || string.IsNullOrEmpty(dotnetToolInfo.Command))
            {
                return new ValueTask<FlowStepResult>(FlowStepResult.Failure("Missing value for name of the component and/or command"));
            }

            var parameterValues = GetAllParameterValues(context, commandObj);
            var envVars = context.GetTelemetryEnvironmentVariables();
            var chosenCategory = context.GetChosenCategory();
            if (!string.IsNullOrEmpty(dotnetToolInfo.Command) && parameterValues.Count != 0 && !string.IsNullOrEmpty(commandObj.Name))
            {
                var componentExecutionString = $"{dotnetToolInfo.Command} {string.Join(" ", parameterValues)}";
                int? exitCode = null;
                AnsiConsole.Status()
                    .Start($"Executing '{dotnetToolInfo.Command}'", statusContext =>
                    {
                        var cliRunner = dotnetToolInfo.IsGlobalTool?
                            DotnetCliRunner.Create(dotnetToolInfo.Command, parameterValues, envVars) :
                            DotnetCliRunner.CreateDotNet(dotnetToolInfo.Command, parameterValues, envVars);
                        exitCode = cliRunner.ExecuteWithCallbacks(
                            (s) => AnsiConsole.Console.MarkupLineInterpolated($"[green]{s}[/]"),
                            (s) => AnsiConsole.Console.MarkupLineInterpolated($"[red]{s}[/]"));
                    });

                _telemetryService.TrackEvent(new CommandExecuteTelemetryEvent(dotnetToolInfo, commandObj, exitCode, chosenCategory));
                if (exitCode != null)
                {
                    if (exitCode != 0)
                    {
                        AnsiConsole.Console.WriteLine($"\nCommand exit code: {exitCode}");
                    }

                    return new ValueTask<FlowStepResult>(FlowStepResult.Success);
                }
            }

            return new ValueTask<FlowStepResult>(FlowStepResult.Failure());
        }

        /// <summary>
        /// Gathers all parameter values from the context for the given command.
        /// </summary>
        /// <param name="context">The flow context.</param>
        /// <param name="commandInfo">The command info object.</param>
        /// <returns>A list of parameter values to pass to the CLI runner.</returns>
        private List<string> GetAllParameterValues(IFlowContext context, CommandInfo commandInfo)
        {
            var parameterValues = new List<string> { commandInfo.Name };
            foreach (var parameter in commandInfo.Parameters)
            {
                var parameterValue = context.GetValue<string>(parameter.Name);
                if (!string.IsNullOrEmpty(parameterValue))
                {
                    parameterValues.Add(parameter.Name);
                    parameterValues.Add(parameterValue);
                }
            }

            return parameterValues;
        }
    }
}
