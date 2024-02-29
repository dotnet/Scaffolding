
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.DotNet.Scaffolding.Helpers.Services;
using Microsoft.DotNet.Tools.Scaffold.Flow;
using Microsoft.DotNet.Tools.Scaffold.Flow.Steps;
using Microsoft.UpgradeAssistant.Cli.Commands;
using Spectre.Console.Cli;
using Spectre.Console.Flow;

public class ScaffoldCommand : BaseCommand<ScaffoldCommand.Settings>
{
    private readonly IFileSystem _fileSystem;
    private readonly ILogger _logger;
    private readonly IEnvironmentService _environmentService;
    public ScaffoldCommand(
        IEnvironmentService environmentService,
        IFileSystem fileSystem,
        IFlowProvider flowProvider,
        ILogger logger)
        : base(flowProvider)
    {
        _environmentService = environmentService;
        _fileSystem = fileSystem;
        _logger = logger;
    }

    public class Settings : CommandSettings
    {
        [CommandArgument(0, "[PROJECT]")]
        public string? Project { get; init; }

        [CommandOption("--non-interactive")]
        public bool NonInteractive { get; init; }
    }

    public override async Task<int> ExecuteAsync(CommandContext context, Settings settings)
    {
        IEnumerable<IFlowStep> flowSteps = new IFlowStep[]
        {
            new StartupFlowStep(_environmentService, _fileSystem, _logger),
            // new SourceProjectFlowStep(_fileSystem),
            // new ScaffoldPickerStep()
        };

        return await RunFlowAsync(flowSteps, settings, settings.NonInteractive);
    }
}

