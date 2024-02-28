using System.Threading;
using System.Threading.Tasks;
using Spectre.Console.Flow;

namespace Microsoft.DotNet.Tools.Scaffold.Flow.Steps;
/// <summary>
/// check for first initialization in ValidateUserInputAsync, 
/// do first time initialization in ValidateUserInputAsync
///   - check for .dotnet-scaffold folder in USER
///   - check for .dotnet-scaffold/manifest.json file
///   - read and check for 1st party .NET scaffolders, update them if needed
/// </summary>
public class StartupFlowStep : IFlowStep
{
    public string Id => throw new System.NotImplementedException();

    public string DisplayName => throw new System.NotImplementedException();

    public ValueTask ResetAsync(IFlowContext context, CancellationToken cancellationToken)
    {
        return new ValueTask();
    }

    public ValueTask<FlowStepResult> RunAsync(IFlowContext context, CancellationToken cancellationToken)
    {
        return new ValueTask<FlowStepResult>(FlowStepResult.Success);
    }

    public ValueTask<FlowStepResult> ValidateUserInputAsync(IFlowContext context, CancellationToken cancellationToken)
    {
        return new ValueTask<FlowStepResult>(FlowStepResult.Failure("always initialize StartupFlow so going straight to RunAsync()")); 
    }
}
