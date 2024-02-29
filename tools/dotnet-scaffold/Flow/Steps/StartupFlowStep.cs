using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.DotNet.Scaffolding.Helpers.Services;
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
    private readonly IEnvironmentService _environmentService;
    private readonly IFileSystem _fileSystem;
    private readonly ILogger _logger;
    private readonly string _dotnetScaffolderFolder = ".dotnet-scaffold";
    private readonly string _manifestFile = "manifest.json";
    public StartupFlowStep(IEnvironmentService environmentService, IFileSystem fileSystem, ILogger logger)
    {
        _environmentService = environmentService;
        _fileSystem = fileSystem;
        _logger = logger;
    }

    public string Id => nameof(StartupFlowStep);

    public string DisplayName => "Startup step";

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
        // check for first initialization
        string userPath =_environmentService.LocalUserFolderPath;
        string dotnetScaffoldFolder = Path.Combine(userPath, _dotnetScaffolderFolder);
        if (!_fileSystem.DirectoryExists(dotnetScaffoldFolder))
        {
            _fileSystem.CreateDirectory(dotnetScaffoldFolder);
        }
        //.dotnet-scaffold folder should now exist
        var manifestFileFullPath = Path.Combine(dotnetScaffoldFolder, _manifestFile);
        if (_fileSystem.FileExists(dotnetScaffoldFolder))
        {
            _fileSystem.WriteAllText(manifestFileFullPath, string.Empty);
        }

        return new ValueTask<FlowStepResult>(FlowStepResult.Success);
    }
}
