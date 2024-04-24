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
            if (commandObj is null)
            {
                throw new System.Exception();
            }

            var commandName = commandObj.Name;
            var parameterValues = GetAllParameterValues(context, commandObj);
            if (!string.IsNullOrEmpty(componentName) && parameterValues.Count != 0 && !string.IsNullOrEmpty(commandName))
            {
                var componentExecutionString = $"{componentName} {commandName} {string.Join(" ", parameterValues)}";
                parameterValues.Insert(0, commandName);
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
                    AnsiConsole.Console.WriteLine($"Command exit code - {exitCode}");
                    if (exitCode == 0)
                    {
                        AnsiConsole.Console.MarkupLine($"Command stdout :\n[green]{stdOut}[/]");
                    }
                    else
                    {
                        AnsiConsole.Console.MarkupLine($"Command stderr :\n[red]{stdErr}[/]");
                    }

                    return new ValueTask<FlowStepResult>(FlowStepResult.Success);
                }
            }

            return new ValueTask<FlowStepResult>(FlowStepResult.Failure());
        }

        private List<string> GetAllParameterValues(IFlowContext context, CommandInfo commandInfo)
        {
            var allParameters = commandInfo.Parameters;
            var sourceProjectPath = context.GetSourceProjectPath();
            var parameterValues = new List<string>();
            if (!string.IsNullOrEmpty(sourceProjectPath))
            {
                parameterValues.Add("--project");
                parameterValues.Add(sourceProjectPath);
            }

            foreach (var parameter in allParameters)
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
