// Copyright (c) Microsoft Corporation. All rights reserved.
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.MSBuild;
  
namespace Microsoft.DotNet.Scaffolding.Helpers.Services;

/// <summary>
/// Service that manages Roslyn workspace. It ensures that all projects of interest are loaded in the workspace and are up to date.
///     - If user's input path is project we also always open it since we expect some contracts might
///       want to get access to that project.
///     - all other projects loaded to the workspace only per-request to avoid running DT builds on all
///       full solution. When some caller need to ensure project is loaded (for example ProjectService
///       when it creates a new instance of <see cref="IProject"/>) it should call OpenProjectAsync
///       here and that would ensure project is loaded in the workspace.
/// </summary>
public class CodeService : ICodeService, IDisposable
{
    private readonly ILogger _logger;
    private MSBuildWorkspace? _workspace;
    private readonly IAppSettings _settings;

    public CodeService(IAppSettings settings, ILogger logger)
    {
        _logger = logger;
        _settings = settings;
    }

    /// <inheritdoc />
    public async Task<Workspace?> GetWorkspaceAsync()
    {
        return await GetMsBuildWorkspaceAsync(_settings.Workspace().InputPath);
    }

    /// <inheritdoc />
    public bool TryApplyChanges(Solution? solution)
    {
        if (solution is null || _workspace is null)
        {
            return false;
        }

        var success = _workspace?.TryApplyChanges(solution) == true;
        return success;
    }

    private async Task<MSBuildWorkspace?> GetMsBuildWorkspaceAsync(string? path)
    {
        if (string.IsNullOrEmpty(path))
        {
            return _workspace;
        }

        if (_workspace is not null)
        {
            return _workspace;
        }

        var workspace = MSBuildWorkspace.Create(_settings.GlobalProperties);
        workspace.WorkspaceFailed += OnWorkspaceFailed;
        workspace.LoadMetadataForReferencedProjects = true;
        await workspace.OpenProjectAsync(path).ConfigureAwait(false);
        _workspace = workspace;
        return _workspace;
    }

    public async Task OpenProjectAsync(string projectPath)
    {
        if (string.IsNullOrEmpty(projectPath))
        {
            return;
        }

        var workspace = await GetWorkspaceAsync();
        if (workspace is not MSBuildWorkspace msbuildWorkspace)
        {
            return;
        }

        Project? project = null;

        //var project = msbuildWorkspace.CurrentSolution.GetProject(projectPath);
        if (project is not null)
        {
            return;
        }

        try
        {
            await msbuildWorkspace.OpenProjectAsync(projectPath).ConfigureAwait(false);
        }
        catch (Exception)
        {
            //_logger.LogError(ex.ToString());
        }
    }

    private void UnloadWorkspace()
    {
        var workspace = _workspace;
        _workspace = null;

        if (workspace is not null)
        {
            workspace.WorkspaceFailed -= OnWorkspaceFailed;
            workspace.CloseSolution();
            workspace.Dispose();
        }
    }

    public async Task ReloadWorkspaceAsync(string? projectPath)
    {
        UnloadWorkspace();

        await GetMsBuildWorkspaceAsync(_settings.Workspace().InputPath);
        await OpenProjectAsync(projectPath!);
    }

    private void OnWorkspaceFailed(object? sender, WorkspaceDiagnosticEventArgs e)
    {
        var diagnostic = e.Diagnostic!;
        //_logger.LogDebug($"[{diagnostic.Kind}] Problem loading file in MSBuild workspace {diagnostic.Message}");
    }

    public void Dispose()
    {
        UnloadWorkspace();
    }
}
