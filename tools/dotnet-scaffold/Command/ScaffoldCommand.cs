using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.DotNet.Tools.Scaffold.Flow;
using Microsoft.DotNet.Tools.Scaffold.Flow.Steps;
using Microsoft.UpgradeAssistant.Cli.Commands;
using Spectre.Console.Cli;
using Spectre.Console.Flow;

public class ScaffoldCommand : BaseCommand<ScaffoldCommand.Settings>
{
    public ScaffoldCommand(
        IFileSystem fileSystem,
        ILogger logger,
        IFlowProvider flowProvider)
        : base(flowProvider)
    {
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
            new StartupFlowStep(),
            new SourceProjectFlowStep(_fileSystem),
        };

        return await RunFlowAsync(flowSteps, settings, settings.NonInteractive);
    }
}