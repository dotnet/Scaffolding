/*using Microsoft.Build.Evaluation;

internal class DefaultMsbuildProjectAccess : IMsbuildProjectAccess
{
    private readonly string _projectPath;
    private readonly ILogger _logger;
    private readonly CancellationToken _cancellationToken;
    private readonly bool _shouldUnload;

    public DefaultMsbuildProjectAccess(string projectPath, ILogger logger, CancellationToken cancellationToken)
    {
        _projectPath = projectPath ?? string.Empty;
        _logger = logger ?? NullLogger.Instance;
        _cancellationToken = cancellationToken;

        var projects = ProjectCollection.GlobalProjectCollection.GetLoadedProjects(_projectPath);

        _shouldUnload = !projects.Any();
        Project = projects.SingleOrDefault() ?? ProjectCollection.GlobalProjectCollection.LoadProject(_projectPath);
    }

    internal Microsoft.Build.Evaluation.Project Project { get; }

    public async Task<T> RunAsync<T>(Func<Microsoft.Build.Evaluation.Project, CancellationToken, Task<T>> projectAction)
    {
        if (projectAction is null)
        {
            return default!;
        }

        if (Project is null)
        {
            _logger.LogInfo($"Could not find project '{_projectPath}'");
            return default!;
        }

        return await projectAction(Project, _cancellationToken);
    }

    public T Run<T>(Func<Microsoft.Build.Evaluation.Project, CancellationToken, T> projectAction)
    {
        if (projectAction is null)
        {
            return default!;
        }

        if (Project is null)
        {
            _logger.LogInfo($"Could not find project '{_projectPath}'");
            return default!;
        }

        return projectAction(Project, _cancellationToken);
    }

    public void Dispose()
    {
        if (_shouldUnload && Project is not null)
        {
            // Unload it again if we were the ones who loaded it.
            // Unload both, project and project root element, to remove it from strong and weak MSBuild caches.
            ProjectCollection.GlobalProjectCollection.UnloadProject(Project);
            ProjectCollection.GlobalProjectCollection.UnloadProject(Project.Xml);
        }
    }
}
*/
