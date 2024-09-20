// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using Microsoft.DotNet.Scaffolding.Core;
using Microsoft.DotNet.Scaffolding.Core.ComponentModel;
using Microsoft.DotNet.Scaffolding.Internal.CliHelpers;
using Spectre.Console;
using Spectre.Console.Flow;

namespace Microsoft.DotNet.Tools.Scaffold.Flow.Steps
{
    /// <summary>
    /// IFlowStep where we gather all the parameter values, and then execute the chosen/given component (dotnet tool).
    /// Print out stdout/stderr
    /// </summary>
    internal class CommandExecuteFlowStep : IFlowStep
    {
        public string Id => nameof(CommandExecuteFlowStep);

        public string DisplayName => "Command Execute";

        public ValueTask ResetAsync(IFlowContext context, CancellationToken cancellationToken)
        {
            return ValueTask.CompletedTask;
        }

        public ValueTask<FlowStepResult> RunAsync(IFlowContext context, CancellationToken cancellationToken)
        {
            return new ValueTask<FlowStepResult>(FlowStepResult.Success);
        }

        public ValueTask<FlowStepResult> ValidateUserInputAsync(IFlowContext context, CancellationToken cancellationToken)
        {
            //need all 3 things, throw if not found
            var dotnetToolInfo = context.GetComponentObj();
            var commandObj = context.GetCommandObj();
            if (dotnetToolInfo is null || commandObj is null || string.IsNullOrEmpty(dotnetToolInfo.Command))
            {
                return new ValueTask<FlowStepResult>(FlowStepResult.Failure("Missing value for name of the component and/or command"));
            }

            var parameterValues = GetAllParameterValues(context, commandObj);
            if (!string.IsNullOrEmpty(dotnetToolInfo.Command) && parameterValues.Count != 0 && !string.IsNullOrEmpty(commandObj.Name))
            {
                var componentExecutionString = $"{dotnetToolInfo.Command} {string.Join(" ", parameterValues)}";
                int? exitCode = null;
                AnsiConsole.Status()
                    .Start($"Executing '{dotnetToolInfo.Command}'", statusContext =>
                    {
                        var cliRunner = dotnetToolInfo.IsGlobalTool?
                            DotnetCliRunner.Create(dotnetToolInfo.Command, parameterValues) :
                            DotnetCliRunner.CreateDotNet(dotnetToolInfo.Command, parameterValues);
                        exitCode = cliRunner.ExecuteWithCallbacks(
                            (s) => AnsiConsole.Console.MarkupLineInterpolated($"[green]{s}[/]"),
                            (s) => AnsiConsole.Console.MarkupLineInterpolated($"[red]{s}[/]"));
                    });

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
