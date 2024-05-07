// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.DotNet.Scaffolding.ComponentModel;
using Spectre.Console.Flow;

namespace Microsoft.DotNet.Tools.Scaffold.Flow.Steps
{
    internal class ParameterBasedFlowStep : IFlowStep
    {
        public Parameter Parameter { get; set; }
        public ParameterBasedFlowStep? NextStep { get; set; }
        public ParameterBasedFlowStep(Parameter parameter, ParameterBasedFlowStep? nextStep)
        {
            Parameter = parameter;
            NextStep = nextStep;
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
            ParameterDiscovery paraDiscovery = new ParameterDiscovery(Parameter);
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
            }
        }
    }
}
