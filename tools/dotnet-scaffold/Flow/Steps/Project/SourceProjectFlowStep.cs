using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.DotNet.Scaffolding.Helpers.General;
using Microsoft.DotNet.Scaffolding.Helpers.Services;
using Microsoft.DotNet.Scaffolding.Helpers.Services.Environment;
using Spectre.Console;
using Spectre.Console.Flow;

namespace Microsoft.DotNet.Tools.Scaffold.Flow.Steps.Project;

internal class SourceProjectFlowStep : IFlowStep
{
    private readonly IAppSettings _appSettings;
    private readonly IFileSystem _fileSystem;
    private readonly IEnvironmentService _environmentService;
    private readonly ILogger _logger;
    public SourceProjectFlowStep(
        IAppSettings appSettings,
        IEnvironmentService environment,
        IFileSystem fileSystem,
        ILogger logger)
    {
        _appSettings = appSettings;
        _fileSystem = fileSystem;
        _environmentService = environment;
        _logger = logger;
    }

    /// <inheritdoc />
    public string Id => nameof(SourceProjectFlowStep);

    /// <inheritdoc />
    public string DisplayName => "Source Project";

    /// <inheritdoc />
    public ValueTask<FlowStepResult> ValidateUserInputAsync(IFlowContext context, CancellationToken cancellationToken)
    {
        var projectPath = context.GetSourceProjectPath();
        if (string.IsNullOrEmpty(projectPath))
        {
            var settings = context.GetCommandSettings();
            projectPath = settings?.Project;
        }

        if (string.IsNullOrEmpty(projectPath))
        {
            return new ValueTask<FlowStepResult>(FlowStepResult.Failure("Source project is needed!"));
        }

        if (!projectPath.IsCSharpProject())
        {
            return new ValueTask<FlowStepResult>(FlowStepResult.Failure($"Project path is invalid '{projectPath}'"));
        }

        if (!Path.IsPathRooted(projectPath))
        {
            projectPath = Path.GetFullPath(Path.Combine(_environmentService.CurrentDirectory, projectPath.Trim(Path.DirectorySeparatorChar)));
        }

        if (!_fileSystem.FileExists(projectPath))
        {
            return new ValueTask<FlowStepResult>(FlowStepResult.Failure(string.Format("Project file '{0}' does not exist", projectPath)));
        }

        SelectSourceProject(context, projectPath);
        return new ValueTask<FlowStepResult>(FlowStepResult.Success);
    }

    /// <inheritdoc />
    public ValueTask<FlowStepResult> RunAsync(IFlowContext context, CancellationToken cancellationToken)
    {
        var settings = context.GetCommandSettings();
        var path = settings?.Project;
        if (string.IsNullOrEmpty(path))
        {
            path = _environmentService.CurrentDirectory;
        }

        if (!Path.IsPathRooted(path))
        {
            path = Path.GetFullPath(Path.Combine(_environmentService.CurrentDirectory, path.Trim(Path.DirectorySeparatorChar)));
        }

        var workingDir = _environmentService.CurrentDirectory;
        if (path.EndsWith(".sln"))
        {
            workingDir = Path.GetDirectoryName(path)!;
        }
        else if (_fileSystem.DirectoryExists(path))
        {
            workingDir = path;
        }

        ProjectDiscovery projectDiscovery = new(_fileSystem, workingDir);
        var projectPath = projectDiscovery.Discover(context, path);
        if (projectDiscovery.State.IsNavigation())
        {
            return new ValueTask<FlowStepResult>(new FlowStepResult { State = projectDiscovery.State });
        }

        if (projectPath is not null)
        {
            SelectSourceProject(context, projectPath);
            return new ValueTask<FlowStepResult>(FlowStepResult.Success);
        }

        AnsiConsole.WriteLine("No projects found in current directory");
        return new ValueTask<FlowStepResult>(FlowStepResult.Failure());
    }

    /// <inheritdoc />
    public ValueTask ResetAsync(IFlowContext context, CancellationToken cancellationToken)
    {
        context.Unset(FlowContextProperties.SourceProjectPath);
        context.Unset(FlowContextProperties.SourceProject);
        return new ValueTask();
    }

    private void SelectSourceProject(IFlowContext context, string projectPath)
    {
        if (!string.IsNullOrEmpty(projectPath))
        {
            context.Set(new FlowProperty(
                FlowContextProperties.SourceProjectPath,
                projectPath,
                FlowContextProperties.SourceProjectDisplay,
                isVisible: true));
            _appSettings.Workspace().InputPath = projectPath;

/*            IProjectService projectService = AnsiConsole
                .Status()
                .WithSpinner()
                .Start("Gathering project information!", statusContext =>
                {
                    return new ProjectService(projectPath, _logger);
                });

            context.Set(new FlowProperty(
                    FlowContextProperties.SourceProject,
                    projectService,
                    isVisible: false));*/

            ICodeService codeService = new CodeService(_appSettings, _logger);
            context.Set(new FlowProperty(
                    FlowContextProperties.CodeService,
                    codeService,
                    isVisible: false));
        }
    }
}


