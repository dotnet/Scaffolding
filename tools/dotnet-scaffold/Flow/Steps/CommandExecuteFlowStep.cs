// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.DotNet.Scaffolding.ComponentModel;
using Microsoft.DotNet.Scaffolding.Helpers.General;
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
            var componentName = context.GetComponentObj()?.Command;
            var commandObj = context.GetCommandObj();
            if (commandObj is null || string.IsNullOrEmpty(componentName))
            {
                return new ValueTask<FlowStepResult>(FlowStepResult.Failure("Missing value for name of the component and/or command"));
            }

            var parameterValues = GetAllParameterValues(context, commandObj);
            if (!string.IsNullOrEmpty(componentName) && parameterValues.Count != 0 && !string.IsNullOrEmpty(commandObj.Name))
            {
                var componentExecutionString = $"{componentName} {string.Join(" ", parameterValues)}";
                string? stdOut = null, stdErr = null;
                int? exitCode = null;
                AnsiConsole.Status().WithSpinner()
                    .Start($"Executing '{componentExecutionString}'", statusContext =>
                    {
                        var cliRunner = DotnetCliRunner.Create(componentName, parameterValues);
                        exitCode = cliRunner.ExecuteAndCaptureOutput(out stdOut, out stdErr);
                    });

                if (exitCode != null && (!string.IsNullOrEmpty(stdOut) || string.IsNullOrEmpty(stdErr)))
                {
                    AnsiConsole.Console.WriteLine($"\nCommand executed : '{componentExecutionString}'");
                    AnsiConsole.Console.WriteLine($"\nCommand exit code - {exitCode}");
                    if (exitCode == 0)
                    {
                        AnsiConsole.Console.MarkupLine($"\nCommand stdout :\n[lightgreen]{stdOut}[/]");
                    }
                    else
                    {
                        AnsiConsole.Console.MarkupLine($"\nCommand stderr :\n[lightred]{stdErr}[/]");
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
