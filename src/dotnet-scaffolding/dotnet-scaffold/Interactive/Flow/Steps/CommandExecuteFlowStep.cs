// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using System.Collections.Generic;
using System.CommandLine;
using System.Threading.Tasks;
using Microsoft.DotNet.Scaffolding.Core.Builder;
using Microsoft.DotNet.Scaffolding.Core.ComponentModel;
using Microsoft.DotNet.Scaffolding.Internal.CliHelpers;
using Microsoft.DotNet.Scaffolding.Internal.Services;
using Microsoft.DotNet.Scaffolding.Internal.Telemetry;
using Microsoft.DotNet.Tools.Scaffold.Telemetry;
using Spectre.Console;
using Spectre.Console.Flow;

namespace Microsoft.DotNet.Tools.Scaffold.Interactive.Flow.Steps
{
    /// <summary>
    /// IFlowStep where we gather all the parameter values, and then execute the chosen/given component (dotnet tool).
    /// Prints out stdout/stderr and tracks telemetry for the execution.
    /// </summary>
    internal class CommandExecuteFlowStep : IFlowStep
    {
        private readonly ITelemetryService _telemetryService;
        private readonly IScaffoldRunner _scaffoldRunnner;

        /// <summary>
        /// Initializes a new instance of the <see cref="CommandExecuteFlowStep"/> class.
        /// </summary>
        /// <param name="telemetryService">The telemetry service to use for tracking events.</param>
        /// <param name="scaffoldRunner">The command runner</param>
        public CommandExecuteFlowStep(ITelemetryService telemetryService, IScaffoldRunner scaffoldRunner)
        {
            _telemetryService = telemetryService;
            _scaffoldRunnner = scaffoldRunner;
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
        public async ValueTask<FlowStepResult> ValidateUserInputAsync(IFlowContext context, CancellationToken cancellationToken)
        {
            // Need all 3 things, throw if not found
            DotNetToolInfo? dotnetToolInfo = context.GetComponentObj();
            CommandInfo? commandInfo = context.GetCommandObj();
            if (dotnetToolInfo is null || commandInfo is null || string.IsNullOrEmpty(dotnetToolInfo.Command))
            {
                return FlowStepResult.Failure("Missing value for name of the component and/or command");
            }

            List<string> parameterValues = GetAllParameterValues(context, commandInfo);
            IDictionary<string, string>? envVars = context.GetTelemetryEnvironmentVariables();
            string? chosenCategory = context.GetChosenCategory();
            if (!string.IsNullOrEmpty(dotnetToolInfo.Command) && parameterValues.Count != 0 && !string.IsNullOrEmpty(commandInfo.Name))
            {
                // TODO when the aspnet is folded into dotnet scaffold, this will be refactored
                if (commandInfo.IsCommandAnAspireCommand())
                {
                    // Build the argument list for System.CommandLine
                    parameterValues.Insert(0, "aspire");

                    if (_scaffoldRunnner is null || _scaffoldRunnner is not ScaffoldRunner runner || runner.RootCommand is null)
                    {
                        return FlowStepResult.Failure("Aspire command infrastructure not available.");
                    }

                    // Invoke the command directly (async)
                    int aspireExitCode = await runner.RootCommand.InvokeAsync([.. parameterValues], cancellationToken: cancellationToken);

                    _telemetryService.TrackEvent(new CommandExecuteTelemetryEvent(dotnetToolInfo, commandInfo, aspireExitCode, chosenCategory));
                    if (aspireExitCode != 0)
                    {
                        AnsiConsole.Console.WriteLine($"\nAspire command exit code: {aspireExitCode}");
                    }
                    return FlowStepResult.Success;
                }
                //asp.net
                else
                {
                    string command = dotnetToolInfo.Command;
                    string componentExecutionString = $"{command} {string.Join(" ", parameterValues)}";
                    int? exitCode = null;
                    AnsiConsole.Status()
                        .Start($"Executing '{command}'", statusContext =>
                        {
                            var cliRunner = dotnetToolInfo.IsGlobalTool ?
                                DotnetCliRunner.Create(command, parameterValues, envVars) :
                                DotnetCliRunner.CreateDotNet(command, parameterValues, envVars);
                            exitCode = cliRunner.ExecuteWithCallbacks(
                                (s) => AnsiConsole.Console.MarkupLineInterpolated($"[green]{s}[/]"),
                                (s) => AnsiConsole.Console.MarkupLineInterpolated($"[red]{s}[/]"));
                        });

                    _telemetryService.TrackEvent(new CommandExecuteTelemetryEvent(dotnetToolInfo, commandInfo, exitCode, chosenCategory));
                    if (exitCode != null)
                    {
                        if (exitCode != 0)
                        {
                            AnsiConsole.Console.WriteLine($"\nCommand exit code: {exitCode}");
                        }

                        return FlowStepResult.Success;
                    }
                }
            }
            return FlowStepResult.Failure();
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
