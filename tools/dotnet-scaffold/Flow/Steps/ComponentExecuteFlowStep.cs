// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.DotNet.Scaffolding.Helpers.General;
using Spectre.Console;
using Spectre.Console.Flow;

namespace Microsoft.DotNet.Tools.Scaffold.Flow.Steps
{
    internal class ComponentExecuteFlowStep : IFlowStep
    {
        public string Id => nameof(ComponentExecuteFlowStep);

        public string DisplayName => throw new System.NotImplementedException();

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
            var componentName = context.GetComponentObj(throwIfEmpty: true)?.Command;
            var commandName = context.GetCommandName(throwIfEmpty: true);
            var commandArgValues = context.GetCommandArgValues(throwIfEmpty: true);
            if (!string.IsNullOrEmpty(componentName) && !string.IsNullOrEmpty(commandName) && commandArgValues != null)
            {
                var componentExecutionString = $"{componentName} {commandName} {string.Join(" ", commandArgValues)}";
                string? stdOut = null, stdErr = null;
                int? exitCode = null;
                AnsiConsole.Status().WithSpinner()
                .Start($"Executing '{componentExecutionString}'", statusContext =>
                {
                    var cliRunner = DotnetCliRunner.Create(componentName, commandArgValues);
                    exitCode = cliRunner.ExecuteAndCaptureOutput(out stdOut, out stdErr);
                });

                if (exitCode != null && (!string.IsNullOrEmpty(stdOut) || string.IsNullOrEmpty(stdErr)))
                {
                    AnsiConsole.Console.WriteLine($"Command executed : '{componentExecutionString}'");
                    AnsiConsole.Console.WriteLine($"Command exit code - {exitCode}");
                    AnsiConsole.Console.WriteLine($"Command stdout :\n{stdOut}");
                    return new ValueTask<FlowStepResult>(FlowStepResult.Success);
                }
            }

            return new ValueTask<FlowStepResult>(FlowStepResult.Failure());
        }
    }
}
