// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Threading;
using System.Threading.Tasks;
using Spectre.Console.Flow;

namespace Microsoft.DotNet.Tools.Scaffold.Flow.Steps
{
    internal class CommandInfoFlowStep : IFlowStep
    {
        public string Id => nameof(CommandInfoFlowStep);

        public string DisplayName => "Command Name";

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
            throw new NotImplementedException();
        }
    }
}
