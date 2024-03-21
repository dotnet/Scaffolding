// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

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
            throw new System.NotImplementedException();
        }

        public ValueTask<FlowStepResult> RunAsync(IFlowContext context, CancellationToken cancellationToken)
        {
            throw new System.NotImplementedException();
        }

        public ValueTask<FlowStepResult> ValidateUserInputAsync(IFlowContext context, CancellationToken cancellationToken)
        {
            var commandName = context.GetCommandName();
            var componentName = context.GetComponentName();
            if (string.IsNullOrEmpty(commandName))
            {
                return new ValueTask<FlowStepResult>(FlowStepResult.Failure("A command name for the given scaffolding component is needed!"));
            }

            var allCommands = context.GetCommandInfos();
            var commandInfo = allCommands?.FirstOrDefault(x => x.Name.Equals(commandName, System.StringComparison.OrdinalIgnoreCase));
            if (commandInfo is null)
            {
                return new ValueTask<FlowStepResult>(FlowStepResult.Failure($"Command '{commandName}' not found in component '{componentName}'!"));
            }

            var commandSteps = GetParameterBasedSteps(commandInfo);
            return new ValueTask<FlowStepResult>(FlowStepResult.Success);
        }

        internal List<ParameterBasedFlowStep> GetParameterBasedSteps(CommandInfo commandInfo)
        {
            var allParameters = commandInfo.Parameters?.ToList();
            var allParametersSteps = new List<ParameterBasedFlowStep>();
            allParameters?.ForEach(x =>
            {
                allParametersSteps.Add(new ParameterBasedFlowStep(x));
            });

            if (allParametersSteps.Count == 0)
            {
                //throw exception
            }
            return allParametersSteps;
        }
    }
}
