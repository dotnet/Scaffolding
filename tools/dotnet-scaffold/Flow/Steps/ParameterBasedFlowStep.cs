// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
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
        public ParameterBasedFlowStep(Parameter parameter)
        {
            Parameter = parameter;
        }

        public string Id => nameof(ParameterBasedFlowStep);
        public string DisplayName => Parameter.DisplayName;

        public ValueTask ResetAsync(IFlowContext context, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public ValueTask<FlowStepResult> RunAsync(IFlowContext context, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public ValueTask<FlowStepResult> ValidateUserInputAsync(IFlowContext context, CancellationToken cancellationToken)
        {
            //check for parameter in the context using --name
            var cmdlineArgs = context.GetArgsDict();
            if (cmdlineArgs != null && cmdlineArgs.Count != 0)
            {
                cmdlineArgs.TryGetValue(Parameter.Name, out var cmdlineValues);
                if (cmdlineValues != null && cmdlineValues.Count != 0 && ParameterHelpers.CheckType(Parameter.Type, cmdlineValues))
                {
                    Parameter.Value = cmdlineValues;
                    return new ValueTask<FlowStepResult>(FlowStepResult.Success);
                }
            }

            if (Parameter.Required)
            {
                return new ValueTask<FlowStepResult>(FlowStepResult.Failure($"No value found for required option '{Parameter.Name}'"));
            }
            else
            {
                return new ValueTask<FlowStepResult>(FlowStepResult.Success);
            }
        }
    }
}
