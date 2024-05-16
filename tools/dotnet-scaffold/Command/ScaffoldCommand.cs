
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.DotNet.Scaffolding.Helpers.Services;
using Microsoft.DotNet.Scaffolding.Helpers.Services.Environment;
using Microsoft.DotNet.Tools.Scaffold.Flow.Steps;
using Microsoft.DotNet.Tools.Scaffold.Services;
using Microsoft.UpgradeAssistant.Cli.Commands;
using Spectre.Console.Cli;
using Spectre.Console.Flow;

public class ScaffoldCommand : BaseCommand<ScaffoldCommand.Settings>
{
    private readonly IAppSettings _appSettings;
    private readonly IDotNetToolService _dotnetToolService;
    private readonly IFileSystem _fileSystem;
    private readonly ILogger _logger;
    private readonly IEnvironmentService _environmentService;
    private readonly IHostService _hostService;
    public ScaffoldCommand(
        IAppSettings appSettings,
        IDotNetToolService dotnetToolService,
        IEnvironmentService environmentService,
        IFileSystem fileSystem,
        IFlowProvider flowProvider,
        IHostService hostService,
        ILogger logger)
        : base(flowProvider)
    {
        _appSettings = appSettings;
        _dotnetToolService = dotnetToolService;
        _environmentService = environmentService;
        _fileSystem = fileSystem;
        _hostService = hostService;
        _logger = logger;
    }

    public class Settings : CommandSettings
    {
        [CommandArgument(0, "[COMPONENT]")]
        public string? ComponentName { get; set; }

        [CommandArgument(1, "[COMMAND NAME]")]
        public string? CommandName { get; set; }

        [CommandOption("--non-interactive")]
        public bool NonInteractive { get; init; }
    }

    public override async Task<int> ExecuteAsync(CommandContext context, Settings settings)
    {
        IEnumerable<IFlowStep> flowSteps =
        [
            new StartupFlowStep(_appSettings, _dotnetToolService, _environmentService, _fileSystem, _hostService, _logger),
            new CategoryPickerFlowStep(_logger, _dotnetToolService),
            new CommandPickerFlowStep(_appSettings, _logger, _dotnetToolService, _environmentService, _fileSystem),
            new CommandExecuteFlowStep()
        ];

        return await RunFlowAsync(flowSteps, settings, context.Remaining, settings.NonInteractive, showSelectedOptions: false);
    }
}

