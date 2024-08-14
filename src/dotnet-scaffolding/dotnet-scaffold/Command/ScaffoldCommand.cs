using Microsoft.DotNet.Scaffolding.Internal.Services;
using Microsoft.DotNet.Tools.Scaffold.Flow.Steps;
using Microsoft.DotNet.Tools.Scaffold.Services;
using Microsoft.Extensions.Logging;
using Spectre.Console.Cli;
using Spectre.Console.Flow;

namespace Microsoft.DotNet.Tools.Scaffold.Command;

internal class ScaffoldCommand : BaseCommand<ScaffoldCommand.Settings>
{
    private readonly IDotNetToolService _dotnetToolService;
    private readonly IFileSystem _fileSystem;
    private readonly ILogger _logger;
    private readonly IEnvironmentService _environmentService;
    public ScaffoldCommand(
        IDotNetToolService dotnetToolService,
        IEnvironmentService environmentService,
        IFileSystem fileSystem,
        IFlowProvider flowProvider,
        ILogger<ScaffoldCommand> logger)
        : base(flowProvider)
    {
        _dotnetToolService = dotnetToolService;
        _environmentService = environmentService;
        _fileSystem = fileSystem;
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
            new StartupFlowStep(_dotnetToolService, _environmentService, _fileSystem, _logger),
            new CategoryPickerFlowStep(_logger, _dotnetToolService),
            new CommandPickerFlowStep(_logger, _dotnetToolService, _environmentService, _fileSystem),
            new CommandExecuteFlowStep()
        ];

        return await RunFlowAsync(flowSteps, settings, context.Remaining, settings.NonInteractive, showSelectedOptions: false);
    }
}

