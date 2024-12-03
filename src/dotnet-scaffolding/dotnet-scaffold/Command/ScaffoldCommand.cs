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
    private readonly IFirstTimeUseNoticeSentinel _firstTimeUseNoticeSentinel;

    public ScaffoldCommand(
        IDotNetToolService dotnetToolService,
        IEnvironmentService environmentService,
        IFileSystem fileSystem,
        IFlowProvider flowProvider,
        ILogger<ScaffoldCommand> logger,
        ITelemetryService telemetry,
        IFirstTimeUseNoticeSentinel firstTimeUseNoticeSentinel)
        : base(flowProvider, telemetry)
    {
        _dotnetToolService = dotnetToolService;
        _environmentService = environmentService;
        _fileSystem = fileSystem;
        _logger = logger;
        _firstTimeUseNoticeSentinel = firstTimeUseNoticeSentinel;
    }

    public class Settings : CommandSettings
    {
        [CommandArgument(0, "[COMPONENT]")]
        public string? ComponentName { get; set; }

        [CommandArgument(1, "[COMMAND NAME]")]
        public string? CommandName { get; set; }

        [CommandOption("--non-interactive")]
        public bool NonInteractive { get; init; }

        [CommandOption("--verbose")]
        public bool Verbose { get; init; }

        [CommandOption("--log-to-file")]
        public bool LogToFile { get; init; }
    }

    public override async Task<int> ExecuteAsync(CommandContext context, Settings settings)
    {
        IEnumerable<IFlowStep> flowSteps =
        [
            new StartupFlowStep(_dotnetToolService, _environmentService, _fileSystem, _logger, _firstTimeUseNoticeSentinel),
            new CategoryPickerFlowStep(_logger, _dotnetToolService),
            new CommandPickerFlowStep(_logger, _dotnetToolService, _environmentService, _fileSystem),
            new CommandExecuteFlowStep(TelemetryService, logToFile: settings.LogToFile, verboseOutput: settings.Verbose)
        ];

        var flowResult = await RunFlowAsync(flowSteps, settings, context.Remaining, settings.NonInteractive, showSelectedOptions: false);
        //wait on the telemetry task to finish
        TelemetryService.Flush();
        return flowResult;
    }
}

