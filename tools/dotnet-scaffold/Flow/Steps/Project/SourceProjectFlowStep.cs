using System.Threading;
using System.Threading.Tasks;
using Spectre.Console.Flow;

namespace Microsoft.DotNet.Tools.Scaffold.Flow.Steps.Project;

public class SourceProjectFlowStep : IFlowStep
{
    public string Id => throw new System.NotImplementedException();

    public string DisplayName => throw new System.NotImplementedException();

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
        throw new System.NotImplementedException();
    }
}
