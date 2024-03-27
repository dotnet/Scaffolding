// Copyright (c) Microsoft Corporation. All rights reserved.
using System.Composition;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.MSBuild;
using Microsoft.DotNet.Scaffolding.Helpers.General;
using Microsoft.DotNet.Scaffolding.Helpers.Services;

namespace Microsoft.UpgradeAssistant.Cli.Slices.Services.Code;

/// <summary>
/// Service that manages Roslyn workspace. It ensures that all projects of interest are loaded in the workspace and are up to date.
///     - If user's input path is project we also always open it since we expect some contracts might
///       want to get access to that project.
///     - all other projects loaded to the workspace only per-request to avoid running DT builds on all
///       full solution. When some caller need to ensure project is loaded (for example ProjectService
///       when it creates a new instance of <see cref="IProject"/>) it should call OpenProjectAsync
///       here and that would ensure project is loaded in the workspace.
/// </summary>
internal class CodeService : ICodeService, IDisposable
{
    private readonly ILogger _logger;
    private MSBuildWorkspace? _workspace;
    private IDictionary<string, string> _properties;

    public CodeService(IDictionary<string, string> properties, ILogger logger)
    {
        _logger = logger;
        _properties = properties;
    }

    /// <inheritdoc />
    public async ValueTask<Workspace?> GetWorkspaceAsync(CancellationToken cancellationToken)
    {
        return await GetMsBuildWorkspaceAsync("", cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public ValueTask<bool> TryApplyChangesAsync(Solution? solution, CancellationToken cancellationToken)
    {
        if (solution is null || _workspace is null)
        {
            return new ValueTask<bool>(false);
        }

        var success = _workspace?.TryApplyChanges(solution) == true;

        return new ValueTask<bool>(success);
    }

    private async ValueTask<MSBuildWorkspace?> GetMsBuildWorkspaceAsync(string? path, CancellationToken token)
    {
        if (string.IsNullOrEmpty(path))
        {
            return _workspace;
        }

        if (_workspace is not null)
        {
            return _workspace;
        }

        var workspace = MSBuildWorkspace.Create(_properties);
        workspace.WorkspaceFailed += OnWorkspaceFailed;
        var project = await workspace.OpenProjectAsync(path, cancellationToken: token).ConfigureAwait(false);
        project.Build();
        _workspace = workspace;
        return _workspace;
    }

    public async ValueTask OpenProjectAsync(string projectPath, CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(projectPath))
        {
            return;
        }

        var workspace = await GetWorkspaceAsync(cancellationToken).ConfigureAwait(false);
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
            await msbuildWorkspace.OpenProjectAsync(projectPath, cancellationToken: cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex)
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

    public async ValueTask ReloadWorkspaceAsync(string? projectPath, CancellationToken cancellationToken)
    {
        UnloadWorkspace();

        await GetMsBuildWorkspaceAsync(
            "",
            cancellationToken).ConfigureAwait(false);

        await OpenProjectAsync(projectPath!, cancellationToken).ConfigureAwait(false);
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
