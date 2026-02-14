using Microsoft.DotNet.Scaffolding.Core.Helpers;
using Microsoft.DotNet.Scaffolding.Core.Model;
using Microsoft.DotNet.Scaffolding.Internal.Services;
using Microsoft.DotNet.Scaffolding.Internal.Telemetry;
using Microsoft.DotNet.Tools.Scaffold.Helpers;
using Microsoft.DotNet.Tools.Scaffold.Services;
using Microsoft.Extensions.Logging;
using Spectre.Console;
using Spectre.Console.Flow;

namespace Microsoft.DotNet.Tools.Scaffold.Interactive.Flow.Steps;

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

                // Detect project TFM to determine if Aspire scaffolders should be available
                statusContext.Status = "Detecting project framework.";
                bool isAspireAvailable = DetectAspireAvailability();
                context.Set(new FlowProperty(
                    FlowContextProperties.IsAspireAvailable,
                    isAspireAvailable,
                    isVisible: false));

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

    /// <summary>
    /// Detects whether Aspire scaffolders should be available based on project TFMs in the current directory.
    /// Aspire scaffolders are not available for .NET 8 projects.
    /// </summary>
    /// <returns>True if Aspire should be available (no .NET 8 only projects found), false otherwise.</returns>
    private bool DetectAspireAvailability()
    {
        try
        {
            var workingDirectory = _environmentService.CurrentDirectory;
            if (!_fileSystem.DirectoryExists(workingDirectory))
            {
                return true; // Default to available if can't determine
            }

            // Find .csproj files in current directory and subdirectories
            var projects = _fileSystem.EnumerateFiles(workingDirectory, "*.csproj", SearchOption.AllDirectories).ToList();
            if (projects.Count == 0)
            {
                return true; // No projects found, default to available
            }

            // Check each project's TFM - if any is .NET 8 only, don't show Aspire
            foreach (var projectPath in projects)
            {
                TargetFramework? targetFramework = TargetFrameworkHelpers.GetTargetFrameworkForProject(projectPath);
                if (targetFramework == TargetFramework.Net8)
                {
                    return false; // .NET 8 project found, Aspire not available
                }
            }

            return true; // No .NET 8 only projects, Aspire is available
        }
        catch
        {
            return true; // On error, default to available
        }
    }
}
