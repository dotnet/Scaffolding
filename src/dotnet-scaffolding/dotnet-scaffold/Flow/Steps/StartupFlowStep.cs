using Microsoft.DotNet.Scaffolding.Internal.Services;
using Microsoft.DotNet.Scaffolding.Internal.Telemetry;
using Microsoft.DotNet.Tools.Scaffold.Helpers;
using Microsoft.DotNet.Tools.Scaffold.Services;
using Microsoft.Extensions.Logging;
using Spectre.Console;
using Spectre.Console.Flow;

namespace Microsoft.DotNet.Tools.Scaffold.Flow.Steps;

/// <summary>
/// Startup flow step that performs first-time initialization, telemetry setup, and argument parsing.
/// Initializes MSBuild, gathers environment variables, and prepares the context for further steps.
/// </summary>
internal class StartupFlowStep : IFlowStep
{
    private readonly IEnvironmentService _environmentService;
    private readonly IDotNetToolService _dotnetToolService;
    private readonly IFileSystem _fileSystem;
    private readonly ILogger _logger;
    private readonly IFirstTimeUseNoticeSentinel _firstTimeUseNoticeSentinel;
    private readonly bool _initializeMsbuild;

    /// <summary>
    /// Initializes a new instance of the <see cref="StartupFlowStep"/> class.
    /// </summary>
    public StartupFlowStep(
        IDotNetToolService dotnetToolService,
        IEnvironmentService environmentService,
        IFileSystem fileSystem,
        ILogger logger,
        IFirstTimeUseNoticeSentinel firstTimeUseNoticeSentinel,
        bool initializeMsbuild = true)
    {
        _dotnetToolService = dotnetToolService;
        _environmentService = environmentService;
        _fileSystem = fileSystem;
        _logger = logger;
        _firstTimeUseNoticeSentinel = firstTimeUseNoticeSentinel;
        _initializeMsbuild = initializeMsbuild;
    }

    /// <inheritdoc/>
    public string Id => nameof(StartupFlowStep);
    /// <inheritdoc/>
    public string DisplayName => "Startup step";

    /// <inheritdoc/>
    public ValueTask ResetAsync(IFlowContext context, CancellationToken cancellationToken)
    {
        return new ValueTask();
    }

    /// <inheritdoc/>
    public ValueTask<FlowStepResult> RunAsync(IFlowContext context, CancellationToken cancellationToken)
    {
        return new ValueTask<FlowStepResult>(FlowStepResult.Success);
    }

    /// <inheritdoc/>
    public ValueTask<FlowStepResult> ValidateUserInputAsync(IFlowContext context, CancellationToken cancellationToken)
    {
        AnsiConsole.Status()
            .WithSpinner()
            .Start("Initializing dotnet-scaffold", statusContext =>
            {
                statusContext.Refresh();
                var envVars = InitializeFirstTimeTelemetry();
                SelectTelemetryEnvironmentVariables(context, envVars);
                // Initialize 1st party components (dotnet tools)
                statusContext.Status = "Getting ready";
                new FirstPartyComponentInitializer(_logger, _dotnetToolService).Initialize(envVars);
                statusContext.Status = "Done\n";
                // Parse args passed
                statusContext.Status = "Parsing args.";
                var remainingArgs = context.GetRemainingArgs();
                if (remainingArgs != null)
                {
                    var argDict = CliHelpers.ParseILookup(remainingArgs.Parsed);
                    if (argDict != null)
                    {
                        SelectCommandArgs(context, argDict);
                    }
                }

                statusContext.Status = "Done\n";
            });

        return new ValueTask<FlowStepResult>(FlowStepResult.Success);
    }

    /// <summary>
    /// Initializes telemetry environment variables for first-time use.
    /// </summary>
    private IDictionary<string, string> InitializeFirstTimeTelemetry()
    {
        var telemetryEnvironmentVariables = new Dictionary<string, string>();
        if (_environmentService.GetEnvironmentVariableAsBool(TelemetryConstants.TELEMETRY_OPTOUT))
        {
            telemetryEnvironmentVariables.Add(TelemetryConstants.DOTNET_SCAFFOLD_TELEMETRY_STATE, TelemetryConstants.TELEMETRY_STATE_DISABLED);
        }
        else if (_firstTimeUseNoticeSentinel.SkipFirstTimeExperience || _firstTimeUseNoticeSentinel.Exists())
        {
            telemetryEnvironmentVariables.Add(TelemetryConstants.DOTNET_SCAFFOLD_TELEMETRY_STATE, TelemetryConstants.TELEMETRY_STATE_ENABLED);
        }
        else
        {
            AnsiConsole.Write(new Markup(_firstTimeUseNoticeSentinel.Title.ToHeader()));
            AnsiConsole.WriteLine();
            AnsiConsole.WriteLine();
            AnsiConsole.WriteLine(_firstTimeUseNoticeSentinel.DisclosureText);
            AnsiConsole.WriteLine();
            AnsiConsole.WriteLine("Press any key to continue...");
            Console.ReadKey();
            _firstTimeUseNoticeSentinel.CreateIfNotExists();
            telemetryEnvironmentVariables.Add(TelemetryConstants.DOTNET_SCAFFOLD_TELEMETRY_STATE, TelemetryConstants.TELEMETRY_STATE_DISABLED);
        }

        return telemetryEnvironmentVariables;
    }

    /// <summary>
    /// Sets command arguments in the flow context.
    /// </summary>
    private void SelectCommandArgs(IFlowContext context, IDictionary<string, List<string>> args)
    {
        if (args is not null)
        {
            context.Set(new FlowProperty(
                FlowContextProperties.CommandArgs,
                args,
                isVisible: false));
        }
    }

    /// <summary>
    /// Sets telemetry environment variables in the flow context.
    /// </summary>
    private void SelectTelemetryEnvironmentVariables(IFlowContext context, IDictionary<string, string> envVars)
    {
        if (envVars is not null)
        {
            context.Set(new FlowProperty(
                FlowContextProperties.TelemetryEnvironmentVariables,
                envVars,
                isVisible: false));
        }
    }
}
