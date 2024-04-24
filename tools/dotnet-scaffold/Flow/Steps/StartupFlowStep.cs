using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.DotNet.Scaffolding.ComponentModel;
using Microsoft.DotNet.Scaffolding.Helpers.General;
using Microsoft.DotNet.Scaffolding.Helpers.Services;
using Microsoft.DotNet.Scaffolding.Helpers.Services.Environment;
using Spectre.Console;
using Spectre.Console.Flow;

namespace Microsoft.DotNet.Tools.Scaffold.Flow.Steps;
/// <summary>
/// check for first initialization in ValidateUserInputAsync, 
/// do first time initialization in ValidateUserInputAsync
///   - check for .dotnet-scaffold folder in USER
///   - check for .dotnet-scaffold/manifest.json file
///   - initialize msbuild
///   - read and check for 1st party .NET scaffolders, update them if needed
/// </summary>
public class StartupFlowStep : IFlowStep
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
            .Start("Initializing dotnet-scaffold", async statusContext =>
            {
                statusContext.Refresh();
                // check for first initialization
                statusContext.Status = "Checking user files!";
                if (_initializeMsbuild)
                {
                    var workspaceSettings = new WorkspaceSettings();
                    _appSettings.AddSettings("workspace", workspaceSettings);

                    statusContext.Status = "Initializing msbuild!";
                    new MsBuildInitializer(_logger).Initialize();
                    statusContext.Status = "DONE\n";
                }

                statusContext.Status = "Gathering environment variables!";
                var environmentVariableProvider = new EnvironmentVariablesStartup(_hostService, _environmentService, _appSettings);
                await environmentVariableProvider.StartupAsync();
                statusContext.Status = "DONE\n";
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

        //read manifest file and update the manifest context var, will use this after project picker.
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

    private void SelectComponents(IFlowContext context, IList<DotNetToolInfo>? components)
    {
        if (components != null)
        {
            context.Set(new FlowProperty(
                FlowContextProperties.DotnetToolComponents,
                components,
                isVisible: false));
        }
    }
}
