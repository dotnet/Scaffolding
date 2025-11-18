// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using System.CommandLine;
using Microsoft.DotNet.Scaffolding.Core.Builder;
using Microsoft.DotNet.Scaffolding.Core.ComponentModel;
using Microsoft.DotNet.Scaffolding.Core.Logging;
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
        private readonly IScaffoldRunner _scaffoldRunner;
        private readonly IScaffolderLogger _scaffolderLogger;

        /// <summary>
        /// Initializes a new instance of the <see cref="CommandExecuteFlowStep"/> class.
        /// </summary>
        /// <param name="telemetryService">The telemetry service to use for tracking events.</param>
        /// <param name="scaffoldRunner">The command runner</param>
        /// <param name="scaffolderLogger">Scaffolder logger for console output.</param>
        public CommandExecuteFlowStep(ITelemetryService telemetryService, IScaffoldRunner scaffoldRunner, IScaffolderLogger scaffolderLogger)
        {
            _telemetryService = telemetryService;
            _scaffoldRunner = scaffoldRunner;
            _scaffolderLogger = scaffolderLogger;
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
                if (commandInfo.IsCommandAnAspireCommand())
                {
                    // Build the argument list for System.CommandLine
                    parameterValues.Insert(0, "aspire");

                    if (_scaffoldRunner is null || _scaffoldRunner is not ScaffoldRunner runner || runner.RootCommand is null)
                    {
                        return FlowStepResult.Failure("Aspire command infrastructure not available.");
                    }

                    // Invoke the command directly (async)
                    int aspireExitCode = await AnsiConsole.Status()
                        .WithSpinner()
                        .StartAsync($"[yellow]Scaffolding...[/]", async context =>
                        {
                            return await runner.RootCommand.InvokeAsync([.. parameterValues], cancellationToken: cancellationToken);
                        });

                    _telemetryService.TrackEvent(new CommandExecuteTelemetryEvent(dotnetToolInfo, commandInfo, aspireExitCode, chosenCategory));
                    if (aspireExitCode != 0)
                    {
                        _scaffolderLogger.LogInformation($"\nAspire command exit code: {aspireExitCode}");
                    }
                    return FlowStepResult.Success;
                }
                //asp.net
                else if (commandInfo.IsCommandAnAspNetCommand())
                {
                    // Build the argument list for System.CommandLine
                    parameterValues.Insert(0, "aspnet");
                    if (_scaffoldRunner is null || _scaffoldRunner is not ScaffoldRunner runner || runner.RootCommand is null)
                    {
                        return FlowStepResult.Failure("AspNet command infrastructure not available.");
                    }
                    // Invoke the command directly (async)
                    int aspnetExitCode = await AnsiConsole.Status()
                        .WithSpinner()
                        .StartAsync($"[yellow]Scaffolding...[/]", async context =>
                        {
                            await Task.Delay(1000);
                            return await runner.RootCommand.InvokeAsync([.. parameterValues], cancellationToken: cancellationToken);
                        });
                    
                    _telemetryService.TrackEvent(new CommandExecuteTelemetryEvent(dotnetToolInfo, commandInfo, aspnetExitCode, chosenCategory));
                    if (aspnetExitCode != 0)
                    {
                        _scaffolderLogger.LogInformation($"\nAspNet command exit code: {aspnetExitCode}");
                    }
                    return FlowStepResult.Success;
                }
                //third party tools
                else
                {
                    string command = dotnetToolInfo.Command;
                    int exitCode = AnsiConsole.Status()
                        .Start($"Executing '{command}'", statusContext =>
                        {
                            var cliRunner = dotnetToolInfo.IsGlobalTool ?
                                DotnetCliRunner.Create(command, parameterValues, envVars) :
                                DotnetCliRunner.CreateDotNet(command, parameterValues, envVars);
                            return cliRunner.ExecuteWithCallbacks(
                                (s) => AnsiConsole.Console.MarkupLineInterpolated($"[green]{s}[/]"),
                                (s) => AnsiConsole.Console.MarkupLineInterpolated($"[red]{s}[/]"));
                        });

                    _telemetryService.TrackEvent(new CommandExecuteTelemetryEvent(dotnetToolInfo, commandInfo, exitCode, chosenCategory));
                    if (exitCode != 0)
                    {
                        _scaffolderLogger.LogInformation($"\nCommand exit code: {exitCode}");
                    }

                    return FlowStepResult.Success;
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
        private static List<string> GetAllParameterValues(IFlowContext context, CommandInfo commandInfo)
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
