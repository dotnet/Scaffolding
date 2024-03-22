// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.DotNet.Scaffolding.ComponentModel;
using Microsoft.DotNet.Scaffolding.Helpers.Services;
using Microsoft.DotNet.Tools.Scaffold.Flow;
using Spectre.Console.Flow;

namespace Microsoft.DotNet.Tools.Scaffold.Flow.Steps
{
    internal class CommandExecuteFlowStep : IFlowStep
    {
        private readonly ILogger _logger;
        private readonly IDotNetToolService _dotnetToolService;
        public CommandExecuteFlowStep(ILogger logger, IDotNetToolService dotnetToolService)
        {
            _logger = logger;
            _dotnetToolService = dotnetToolService;
        }

        public string Id => nameof(CommandExecuteFlowStep);

        public string DisplayName => "Command Name";

        public ValueTask ResetAsync(IFlowContext context, CancellationToken cancellationToken)
        {
            context.Unset(FlowContextProperties.CommandName);
            return new ValueTask();
        }

        public ValueTask<FlowStepResult> RunAsync(IFlowContext context, CancellationToken cancellationToken)
        {
            var commandName = string.Empty;
            var componentName = context.GetComponentObj()?.Command;
            CommandInfo? commandInfo;
            if (string.IsNullOrEmpty(commandName))
            {
                if (string.IsNullOrEmpty(componentName))
                {
                    throw new Exception();
                }

                CommandDiscovery commandDiscovery = new();
                commandInfo = commandDiscovery.Discover(context);
            }
            else
            {
                var allCommands = context.GetCommandInfos();
                commandInfo = allCommands?.FirstOrDefault(x => x.Name.Equals(commandName, StringComparison.OrdinalIgnoreCase));
            }

            if (commandInfo is null)
            {
                throw new Exception();
            }

            var commandFirstStep = GetFirstParameterBasedStep(commandInfo);
            if (commandFirstStep is null)
            {
                throw new Exception("asdf");
            }

            return new ValueTask<FlowStepResult>(new FlowStepResult { State = FlowStepState.Success, Steps = new List<ParameterBasedFlowStep> { commandFirstStep } });
        }

        public ValueTask<FlowStepResult> ValidateUserInputAsync(IFlowContext context, CancellationToken cancellationToken)
        {
            var commandName = context.GetCommandName();
            var componentName = context.GetComponentObj();
            if (string.IsNullOrEmpty(commandName))
            {
                return new ValueTask<FlowStepResult>(FlowStepResult.Failure("A command name for the given scaffolding component is needed!"));
            }

            var allCommands = context.GetCommandInfos();
            var commandInfo = allCommands?.FirstOrDefault(x => x.Name.Equals(commandName, StringComparison.OrdinalIgnoreCase));
            if (commandInfo is null)
            {
                return new ValueTask<FlowStepResult>(FlowStepResult.Failure($"Command '{commandName}' not found in component '{componentName}'!"));
            }

            var commandFirstStep = GetFirstParameterBasedStep(commandInfo);
            if (commandFirstStep is null)
            {
                throw new Exception("asdf");
            }
            return new ValueTask<FlowStepResult>(new FlowStepResult { State = FlowStepState.Success, Steps = new List<ParameterBasedFlowStep> { commandFirstStep } });
        }

        internal ParameterBasedFlowStep? GetFirstParameterBasedStep(CommandInfo commandInfo)
        {
            ParameterBasedFlowStep? firstParameterStep = null;
            if (commandInfo.Parameters != null && commandInfo.Parameters.Length != 0)
            {
                firstParameterStep = BuildParameterFlowSteps([.. commandInfo.Parameters]);
            }

            return firstParameterStep;
        }

        internal ParameterBasedFlowStep? BuildParameterFlowSteps(List<Parameter> parameters)
        {
            ParameterBasedFlowStep? firstStep = null;
            ParameterBasedFlowStep? previousStep = null;

            foreach (var parameter in parameters)
            {
                var step = new ParameterBasedFlowStep(parameter, null);

                if (firstStep == null)
                {
                    // This is the first step
                    firstStep = step;
                }
                else
                {
                    if (previousStep != null)
                    {
                        // Connect the previous step to this step
                        previousStep.NextStep = step;
                    }
                }

                previousStep = step;
            }

            return firstStep;
        }

        private void SelectCommandName(IFlowContext context, string commandName)
        {
            context.Set(new FlowProperty(
                FlowContextProperties.CommandName,
                commandName,
                "Command Name",
                isVisible: true));
        }
    }
}
