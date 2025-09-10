using System.ComponentModel;
using Microsoft.DotNet.Scaffolding.Internal.Services;
using Microsoft.DotNet.Tools.Scaffold.Flow.Steps;
using Microsoft.DotNet.Tools.Scaffold.Services;
using Microsoft.Extensions.Logging;
using Spectre.Console.Cli;
using Spectre.Console.Flow;

namespace Microsoft.DotNet.Tools.Scaffold.Command;

/// <summary>
/// Implements the main scaffold command, orchestrating the flow of user interaction and code generation.
/// </summary>
internal class ScaffoldCommand : BaseCommand<ScaffoldCommand.Settings>
{
    // Service for managing .NET tools.
    private readonly IDotNetToolService _dotnetToolService;
    // Service for file system operations.
    private readonly IFileSystem _fileSystem;
    // Logger for command output and diagnostics.
    private readonly ILogger _logger;
    // Service for environment-related operations.
    private readonly IEnvironmentService _environmentService;
    // Sentinel for first-time use notice.
    private readonly IFirstTimeUseNoticeSentinel _firstTimeUseNoticeSentinel;

    /// <summary>
    /// Initializes a new instance of the <see cref="ScaffoldCommand"/> class.
    /// </summary>
    /// <param name="dotnetToolService">The .NET tool service.</param>
    /// <param name="environmentService">The environment service.</param>
    /// <param name="fileSystem">The file system service.</param>
    /// <param name="flowProvider">The flow provider.</param>
    /// <param name="logger">The logger instance.</param>
    /// <param name="telemetry">The telemetry service.</param>
    /// <param name="firstTimeUseNoticeSentinel">The first-time use notice sentinel.</param>
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

    /// <summary>
    /// Settings for the scaffold command, including component and command names and options.
    /// </summary>
    public class Settings : CommandSettings
    {
        /// <summary>
        /// Gets or sets the component name to scaffold.
        /// </summary>
        [Description("dotnet-scaffold-aspnet or dotnet-scaffold-aspire")]
        [CommandArgument(0, "[COMPONENT]")]
        public string? ComponentName { get; set; }

        /// <summary>
        /// Gets or sets the command name to execute.
        /// </summary>
        [CommandArgument(1, "[COMMAND NAME]")]
        public string? CommandName { get; set; }

        /// <summary>
        /// Gets a value indicating whether to run in non-interactive mode.
        /// </summary>
        [CommandOption("--non-interactive")]
        public bool NonInteractive { get; init; }
    }

    /// <summary>
    /// Executes the scaffold command asynchronously, running the configured flow steps.
    /// </summary>
    /// <param name="context">The command context.</param>
    /// <param name="settings">The command settings.</param>
    /// <returns>The exit code from the flow execution.</returns>
    public override async Task<int> ExecuteAsync(CommandContext context, Settings settings)
    {
        // Define the sequence of flow steps for the scaffold command.
        IEnumerable<IFlowStep> flowSteps =
        [
            new StartupFlowStep(_dotnetToolService, _environmentService, _fileSystem, _logger, _firstTimeUseNoticeSentinel),
            new CategoryPickerFlowStep(_logger, _dotnetToolService),
            new CommandPickerFlowStep(_logger, _dotnetToolService, _environmentService, _fileSystem),
            new CommandExecuteFlowStep(TelemetryService)
        ];

        // Run the flow and wait for telemetry to flush before returning.
        var flowResult = await RunFlowAsync(flowSteps, settings, context.Remaining, settings.NonInteractive, showSelectedOptions: false);
        TelemetryService.Flush();
        return flowResult;
    }
}

