using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.DotNet.Scaffolding.Helpers.General;
using Microsoft.DotNet.Scaffolding.Helpers.Services;
using Microsoft.DotNet.Scaffolding.Helpers.Services.Environment;
using Microsoft.DotNet.Tools.Scaffold.Services;
using Spectre.Console;
using Spectre.Console.Flow;

namespace Microsoft.DotNet.Tools.Scaffold.Flow.Steps;
/// <summary>
/// do first time initialization in ValidateUserInputAsync
///   - initialize MSBuild instance
///   - gather some specific environment variables
///   - 
///   - 
/// </summary>
internal class StartupFlowStep : IFlowStep
{
    private readonly IAppSettings _appSettings;
    private readonly IEnvironmentService _environmentService;
    private readonly IDotNetToolService _dotnetToolService;
    private readonly IFileSystem _fileSystem;
    private readonly IHostService _hostService;
    private readonly ILogger _logger;
    private readonly bool _initializeMsbuild;
    public StartupFlowStep(
        IAppSettings appSettings,
        IDotNetToolService dotnetToolService,
        IEnvironmentService environmentService,
        IFileSystem fileSystem,
        IHostService hostService,
        ILogger logger,
        bool initializeMsbuild = true)
    {
        _appSettings = appSettings;
        _dotnetToolService = dotnetToolService;
        _environmentService = environmentService;
        _fileSystem = fileSystem;
        _hostService = hostService;
        _logger = logger;
        _initializeMsbuild = initializeMsbuild;
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
        AnsiConsole.Status()
            .WithSpinner()
            .Start("Initializing dotnet-scaffold", statusContext =>
            {
                statusContext.Refresh();
                //add 'workspace' settings
                var workspaceSettings = new WorkspaceSettings();
                _appSettings.AddSettings("workspace", workspaceSettings);
                //initialize 1st party components (dotnet tools)
                statusContext.Status = "Initializing 1st party components (dotnet tools)";
                new FirstPartyComponentInitializer(_logger, _dotnetToolService).Initialize();
                statusContext.Status = "DONE\n";
                //parse args passed
                statusContext.Status = "Parsing args!";
                var remainingArgs = context.GetRemainingArgs();
                if (remainingArgs != null)
                {
                    var argDict = CliHelpers.ParseILookup(remainingArgs.Parsed);
                    if (argDict != null)
                    {
                        SelectCommandArgs(context, argDict);
                    }
                }

                statusContext.Status = "DONE\n";
            });

        return new ValueTask<FlowStepResult>(FlowStepResult.Success);
    }

    private void SelectCommandArgs(IFlowContext context, IDictionary<string, List<string>> args)
    {
        if (args != null)
        {
            context.Set(new FlowProperty(
                FlowContextProperties.CommandArgs,
                args,
                isVisible: false));
        }
    }
}
