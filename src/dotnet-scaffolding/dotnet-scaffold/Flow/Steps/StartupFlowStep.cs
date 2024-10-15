using Microsoft.DotNet.Scaffolding.Internal.Services;
using Microsoft.DotNet.Tools.Scaffold.Services;
using Microsoft.Extensions.Logging;
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
    private readonly IEnvironmentService _environmentService;
    private readonly IDotNetToolService _dotnetToolService;
    private readonly IFileSystem _fileSystem;
    private readonly ILogger _logger;
    private readonly IFirstTimeUseNoticeSentinel _firstTimeUseNoticeSentinel;
    private readonly bool _initializeMsbuild;
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
                InitializeFirstTimeTelemetry();
                //initialize 1st party components (dotnet tools)
                statusContext.Status = "Getting ready";
                new FirstPartyComponentInitializer(_logger, _dotnetToolService).Initialize();
                statusContext.Status = "Done\n";
                //parse args passed
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

    private void InitializeFirstTimeTelemetry()
    {
        if (!(_firstTimeUseNoticeSentinel.SkipFirstTimeExperience || _firstTimeUseNoticeSentinel.Exists()))
        {
            AnsiConsole.Write(new Markup(_firstTimeUseNoticeSentinel.Title.ToHeader()));
            AnsiConsole.WriteLine();
            AnsiConsole.WriteLine();
            AnsiConsole.WriteLine(_firstTimeUseNoticeSentinel.DisclosureText);
            AnsiConsole.WriteLine();
            AnsiConsole.WriteLine("Press any key to continue...");
            Console.ReadKey();

            _firstTimeUseNoticeSentinel.CreateIfNotExists();
        }
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
