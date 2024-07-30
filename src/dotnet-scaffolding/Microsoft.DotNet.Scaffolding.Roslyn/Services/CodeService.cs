// Copyright (c) Microsoft Corporation. All rights reserved.
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.MSBuild;
using Microsoft.DotNet.Scaffolding.Roslyn.Extensions;
using Microsoft.Extensions.Logging;

namespace Microsoft.DotNet.Scaffolding.Roslyn.Services;
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
    private Compilation? _compilation;
    private readonly string _projectPath;
    private bool _initialized;
    private readonly object _initLock = new();

    public CodeService(ILogger logger, string projectPath)
    {
        _logger = logger;
        _projectPath = projectPath;
    }

    private void Initialize()
    {
        lock (_initLock)
        {
            if (_initialized)
            {
                return;
            }

            new MsBuildInitializer(_logger).Initialize();
            _initialized = true;
        }
    }

    private void EnsureInitialized()
    {
        if (!_initialized)
        {
            Initialize();
        }
    }

    /// <inheritdoc />
    public async Task<Workspace?> GetWorkspaceAsync()
    {
        EnsureInitialized();
        return await GetMsBuildWorkspaceAsync();
    }

    /// <inheritdoc />
    public bool TryApplyChanges(Solution? solution)
    {
        EnsureInitialized();
        if (solution is null || _workspace is null)
        {
            return false;
        }

        var success = _workspace?.TryApplyChanges(solution) == true;
        return success;
    }

    private async Task<MSBuildWorkspace?> GetMsBuildWorkspaceAsync(bool refresh = false)
    {
        EnsureInitialized();

        if (_workspace is not null && !refresh)
        {
            return _workspace;
        }

        var workspace = MSBuildWorkspace.Create();
        workspace.WorkspaceFailed += OnWorkspaceFailed;
        workspace.LoadMetadataForReferencedProjects = true;
        await workspace.OpenProjectAsync(_projectPath);
        _workspace = workspace;
        return _workspace;
    }

    public async Task OpenProjectAsync()
    {
        EnsureInitialized();
        if (string.IsNullOrEmpty(_projectPath))
        {
            return;
        }

        var workspace = await GetWorkspaceAsync();
        if (workspace is not MSBuildWorkspace msbuildWorkspace)
        {
            return;
        }

        Project? project = msbuildWorkspace.CurrentSolution.GetProject(_projectPath);
        if (project is not null)
        {
            return;
        }

        try
        {
            await msbuildWorkspace.OpenProjectAsync(_projectPath).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex.ToString());
        }
    }

    private void UnloadWorkspace()
    {
        var workspace = _workspace;
        _workspace = null;
        _compilation = null;
        if (workspace is not null)
        {
            workspace.WorkspaceFailed -= OnWorkspaceFailed;
            workspace.CloseSolution();
            workspace.Dispose();
        }
    }

    public async Task ReloadWorkspaceAsync()
    {
        EnsureInitialized();
        UnloadWorkspace();

        await GetMsBuildWorkspaceAsync(refresh: true);
    }

    //TODO add a debug option to logging.
    private void OnWorkspaceFailed(object? sender, WorkspaceDiagnosticEventArgs e)
    {
        var diagnostic = e.Diagnostic!;
    }

    public void Dispose()
    {
        UnloadWorkspace();
    }

    public async Task<List<ISymbol>> GetAllClassSymbolsAsync()
    {
        EnsureInitialized();
        List<ISymbol> classSymbols = [];
        if (_compilation is null)
        {
            var workspace = await GetWorkspaceAsync();
            var project = workspace?.CurrentSolution?.GetProject(_projectPath);
            if (project is not null)
            {
                _compilation = await project.GetCompilationAsync();
            }
        }

        var compilationClassSymbols = _compilation?.SyntaxTrees.SelectMany(tree =>
        {
            var model = _compilation.GetSemanticModel(tree);
            var allNodes = tree.GetRoot().DescendantNodes();
            return tree.GetRoot().DescendantNodes().OfType<ClassDeclarationSyntax>().Select(classSyntax =>
            {
                var classSymbol = model.GetDeclaredSymbol(classSyntax);
                return classSymbol;
            });
        }).Append(_compilation.GetEntryPoint(CancellationToken.None)?.ContainingType).ToList();

        compilationClassSymbols?.ForEach(x =>
        {
            if (x is not null)
            {
                classSymbols.Add(x);
            }
        });

        return classSymbols;
    }

    public async Task<List<Document>> GetAllDocumentsAsync()
    {
        EnsureInitialized();
        var workspace = await GetWorkspaceAsync();
        var project = workspace?.CurrentSolution?.GetProject(_projectPath);
        if (project is not null)
        {
            return project.Documents.ToList();
        }

        return [];
    }

    public async Task<Document?> GetDocumentAsync(string? documentName)
    {
        EnsureInitialized();
        if (string.IsNullOrWhiteSpace(documentName))
        {
            return null;
        }

        var workspace = await GetWorkspaceAsync();
        var project = workspace?.CurrentSolution?.GetProject(_projectPath);
        if (project is not null)
        {
            return project.Documents.FirstOrDefault(x => x.Name.EndsWith(documentName));
        }

        return null;
    }
}
