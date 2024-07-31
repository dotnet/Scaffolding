// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.DotNet.Scaffolding.Core.ComponentModel;
using Microsoft.DotNet.Scaffolding.Internal.Services;
using Microsoft.DotNet.Scaffolding.Roslyn.Services;
using Microsoft.Extensions.Logging;
using Spectre.Console.Flow;

namespace Microsoft.DotNet.Tools.Scaffold.Flow.Steps
{
    internal class ParameterBasedFlowStep : IFlowStep
    {
        public Parameter Parameter { get; set; }
        public ParameterBasedFlowStep? NextStep { get; set; }
        private readonly IEnvironmentService _environmentService;
        private readonly IFileSystem _fileSystem;
        private readonly ILogger _logger;
        public ParameterBasedFlowStep(
            Parameter parameter,
            ParameterBasedFlowStep? nextStep,
            IEnvironmentService environmentService,
            IFileSystem fileSystem,
            ILogger logger)
        {
            Parameter = parameter;
            NextStep = nextStep;
            _environmentService = environmentService;
            _fileSystem = fileSystem;
            _logger = logger;
        }

        public string Id => nameof(ParameterBasedFlowStep);
        public string DisplayName => Parameter.DisplayName;

        public ValueTask ResetAsync(IFlowContext context, CancellationToken cancellationToken)
        {
            context.Unset(Parameter.Name);
            return new ValueTask();
        }

        public async ValueTask<FlowStepResult> RunAsync(IFlowContext context, CancellationToken cancellationToken)
        {
            ParameterDiscovery paraDiscovery = new ParameterDiscovery(Parameter, _fileSystem, _environmentService);
            var parameterValue = await paraDiscovery.DiscoverAsync(context);
            if (string.Equals(parameterValue, FlowNavigation.BackInputToken, StringComparison.OrdinalIgnoreCase))
            {
                return FlowStepResult.Back;
            }
            else if (paraDiscovery.State.IsNavigation())
            {
                return new FlowStepResult { State = paraDiscovery.State };
            }
            else
            {
                SelectParameter(context, parameterValue ?? string.Empty);
            }

            if (NextStep != null)
            {
                return new FlowStepResult { State = FlowStepState.Success, Steps = new List<ParameterBasedFlowStep> { NextStep } };
            }
            else
            {
                return FlowStepResult.Success;
            }
        }

        public ValueTask<FlowStepResult> ValidateUserInputAsync(IFlowContext context, CancellationToken cancellationToken)
        {
            //check for parameter in the context using --name
            var cmdlineArgs = context.GetArgsDict();
            var commandSettings = context.GetCommandSettings();
            if (cmdlineArgs != null && cmdlineArgs.Count != 0)
            {
                var strippedParameterName = Parameter.Name.Replace("--", string.Empty);
                cmdlineArgs.TryGetValue(strippedParameterName, out var cmdlineValues);
                if (cmdlineValues != null && cmdlineValues.Count != 0 && ParameterHelpers.CheckType(Parameter.Type, cmdlineValues.First()))
                {
                    SelectParameter(context, cmdlineValues.First());
                    if (NextStep != null)
                    {
                        return new ValueTask<FlowStepResult>(new FlowStepResult { State = FlowStepState.Success, Steps = new List<ParameterBasedFlowStep> { NextStep } });
                    }
                    else
                    {
                        return new ValueTask<FlowStepResult>(FlowStepResult.Success);
                    }
                }
            }

            if (Parameter.Required || (commandSettings != null && !commandSettings.NonInteractive))
            {
                return new ValueTask<FlowStepResult>(FlowStepResult.Failure($"No value found for option '{Parameter.Name}'"));
            }
            else if (NextStep != null)
            {
                return new ValueTask<FlowStepResult>(new FlowStepResult { State = FlowStepState.Success, Steps = new List<ParameterBasedFlowStep> { NextStep } });
            }
            else
            {
                return new ValueTask<FlowStepResult>(FlowStepResult.Success);
            }
        }

        private void SelectParameter(IFlowContext context, string parameterValue)
        {
            if (!string.IsNullOrEmpty(parameterValue))
            {
                context.Set(new FlowProperty(
                    Parameter.Name,
                    parameterValue,
                    Parameter.DisplayName,
                    isVisible: true));
                SelectCodeService(context, parameterValue);
            }
        }

        private void SelectCodeService(IFlowContext context, string projectPath)
        {
            var codeService = context.GetCodeService();
            if (Parameter.PickerType is InteractivePickerType.ProjectPicker && codeService is null && !string.IsNullOrEmpty(projectPath))
            {
                codeService = new CodeService(_logger, projectPath);
                context.Set(new FlowProperty(
                    FlowContextProperties.CodeService,
                    codeService));
            }
        }
    }
}
