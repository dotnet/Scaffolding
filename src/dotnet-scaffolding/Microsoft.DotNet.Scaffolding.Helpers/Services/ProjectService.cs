using Microsoft.Build.Evaluation;

namespace Microsoft.DotNet.Scaffolding.Helpers.Services;
public class ProjectService : IProjectService, IDisposable
{
    private readonly string _projectPath;
    private readonly ILogger _logger;
    private readonly bool _shouldUnload;

    public ProjectService(string projectPath, ILogger logger)
    {
        _projectPath = projectPath ?? string.Empty;
        _logger = logger;
        var projects = ProjectCollection.GlobalProjectCollection.GetLoadedProjects(_projectPath);
        _shouldUnload = projects.Count == 0;
        Project = projects.SingleOrDefault() ?? ProjectCollection.GlobalProjectCollection.LoadProject(_projectPath);
        Project.Build();
    }

    public Project Project { get; }

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
