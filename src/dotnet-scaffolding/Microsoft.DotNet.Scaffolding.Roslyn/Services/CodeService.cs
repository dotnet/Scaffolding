// Copyright (c) Microsoft Corporation. All rights reserved.
using System.Diagnostics;
using System.Text;
using System.Text.Json;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.MSBuild;
using Microsoft.CodeAnalysis.Text;
using Microsoft.DotNet.Scaffolding.Roslyn.Extensions;
using Microsoft.DotNet.Scaffolding.Roslyn.Helpers;
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
    private MSBuildWorkspace? _msBuildWorkspace;
    private AdhocWorkspace? _fallbackWorkspace;
    private IDisposable? _workspaceFailedRegistration;
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

        // If we already determined the MSBuildWorkspace can't load the project, return the
        // cached fallback workspace so document-based operations and ProjectModifier work.
        if (_fallbackWorkspace is not null)
        {
            return _fallbackWorkspace;
        }

        var msBuildWorkspace = await GetMsBuildWorkspaceAsync();

        // Happy path: MSBuildWorkspace successfully loaded the project.
        if (msBuildWorkspace?.CurrentSolution?.GetProject(_projectPath) is not null)
        {
            return msBuildWorkspace;
        }

        // Fallback: MSBuildWorkspace didn't load the project (e.g., a preview SDK version
        // such as net11.0 where the SDK resolver fails silently). Build an AdhocWorkspace
        // from the SDK's compiler inputs so that GetAllDocumentsAsync, GetDocumentAsync,
        // and ProjectModifier.RunAsync can still read and write files.
        _fallbackWorkspace = await BuildFallbackWorkspaceAsync();
        return (Workspace?)_fallbackWorkspace ?? msBuildWorkspace;
    }

    /// <inheritdoc />
    public bool TryApplyChanges(Solution? solution)
    {
        EnsureInitialized();
        if (solution is null)
        {
            return false;
        }

        // If the fallback workspace is active the solution came from it.
        // AdhocWorkspace.TryApplyChanges updates in-memory state but does not write to disk,
        // so we manually persist any changed documents before delegating.
        if (_fallbackWorkspace is not null)
        {
            var currentSolution = _fallbackWorkspace.CurrentSolution;
            var solutionChanges = solution.GetChanges(currentSolution);
            foreach (var projectChange in solutionChanges.GetProjectChanges())
            {
                foreach (var changedDocId in projectChange.GetChangedDocuments())
                {
                    var newDoc = solution.GetDocument(changedDocId);
                    if (newDoc?.FilePath is not null)
                    {
                        var sourceText = newDoc.GetTextAsync().GetAwaiter().GetResult();
                        File.WriteAllText(newDoc.FilePath, sourceText.ToString(), Encoding.UTF8);
                    }
                }
            }

            return _fallbackWorkspace.TryApplyChanges(solution);
        }

        return _msBuildWorkspace?.TryApplyChanges(solution) == true;
    }

    private async Task<MSBuildWorkspace?> GetMsBuildWorkspaceAsync(bool refresh = false)
    {
        EnsureInitialized();

        if (_msBuildWorkspace is not null && !refresh)
        {
            return _msBuildWorkspace;
        }

        MSBuildWorkspace? workspace = null;
        try
        {
            workspace = MSBuildWorkspace.Create();
            _workspaceFailedRegistration = workspace.RegisterWorkspaceFailedHandler(OnWorkspaceFailed);
            workspace.LoadMetadataForReferencedProjects = true;
            await workspace.OpenProjectAsync(_projectPath);
            _msBuildWorkspace = workspace;
            return _msBuildWorkspace;
        }
        catch (Exception ex)
        {
            _logger.LogDebug("MSBuildWorkspace failed to open project '{ProjectPath}': {Message}", _projectPath, ex.Message);
            _workspaceFailedRegistration?.Dispose();
            _workspaceFailedRegistration = null;
            workspace?.Dispose();
            return null;
        }
    }

    /// <summary>
    /// Builds an <see cref="AdhocWorkspace"/> using the project's compiler inputs.
    /// Used when <see cref="MSBuildWorkspace"/> cannot load the project (e.g., preview SDK
    /// versions such as net11.0).  Document changes are written to disk manually inside
    /// <see cref="TryApplyChanges"/> before the in-memory workspace state is updated.
    /// </summary>
    private async Task<AdhocWorkspace?> BuildFallbackWorkspaceAsync()
    {
        var projectDirectory = Path.GetDirectoryName(Path.GetFullPath(_projectPath));
        if (string.IsNullOrEmpty(projectDirectory) || !Directory.Exists(projectDirectory))
        {
            return null;
        }

        if (!Path.GetExtension(_projectPath).Equals(".csproj", StringComparison.OrdinalIgnoreCase))
        {
            throw new NotSupportedException("The fallback workspace supports C# projects only.");
        }

        // Use the SDK's reference resolution and compiler command line, not assemblies loaded
        // by the scaffolding tool. This preserves targeting-pack/package references, aliases,
        // language options, conditional symbols, and generated implicit global usings.
        using var properties = await RunFallbackMsBuildAsync(["-getProperty:TargetFramework,TargetFrameworks"]);
        var evaluatedProperties = properties.RootElement.GetProperty("Properties");
        var targetFramework = evaluatedProperties.GetProperty("TargetFramework").GetString();
        if (string.IsNullOrEmpty(targetFramework))
        {
            targetFramework = evaluatedProperties.GetProperty("TargetFrameworks").GetString()?
                .Split(';', StringSplitOptions.RemoveEmptyEntries)
                .OrderBy(MSBuildProjectServiceHelper.ParseFrameworkVersion).FirstOrDefault();
        }

        var arguments = new List<string>
        {
            "-target:Compile",
            "-property:DesignTimeBuild=true",
            "-property:BuildProjectReferences=false",
            "-property:SkipCompilerExecution=true",
            "-property:ProvideCommandLineArgs=true",
            "-getItem:CscCommandLineArgs"
        };
        if (!string.IsNullOrEmpty(targetFramework))
        {
            arguments.Add($"-property:TargetFramework={targetFramework}");
        }
        using var inputs = await RunFallbackMsBuildAsync(arguments);
        var compilerArguments = inputs.RootElement.GetProperty("Items").GetProperty("CscCommandLineArgs")
            .EnumerateArray().Select(item => item.GetProperty("Identity").GetString()!).ToArray();
        if (compilerArguments.Length == 0)
        {
            throw new InvalidOperationException($"The project '{_projectPath}' did not provide C# compiler inputs.");
        }

        var commandLine = CSharpCommandLineParser.Default.Parse(compilerArguments, projectDirectory, sdkDirectory: null);
        var diagnostics = commandLine.Errors.Where(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error).ToArray();
        if (diagnostics.Length > 0)
        {
            throw new InvalidOperationException($"Invalid compiler inputs for '{_projectPath}': {string.Join(Environment.NewLine, diagnostics.Select(diagnostic => diagnostic.ToString()))}");
        }

        var references = commandLine.MetadataReferences.Select(reference =>
            MetadataReference.CreateFromFile(Path.GetFullPath(reference.Reference, projectDirectory), reference.Properties)).ToArray();
        var projectInfo = ProjectInfo.Create(
            ProjectId.CreateNewId(),
            VersionStamp.Create(),
            name: Path.GetFileNameWithoutExtension(_projectPath),
            assemblyName: commandLine.CompilationName ?? Path.GetFileNameWithoutExtension(_projectPath),
            language: LanguageNames.CSharp,
            filePath: _projectPath,
            compilationOptions: commandLine.CompilationOptions,
            parseOptions: commandLine.ParseOptions,
            metadataReferences: references);

        var documents = new List<DocumentInfo>();
        foreach (var source in commandLine.SourceFiles)
        {
            var file = source.Path;
            var text = await File.ReadAllTextAsync(file);
            documents.Add(DocumentInfo.Create(
                DocumentId.CreateNewId(projectInfo.Id),
                Path.GetFileName(file),
                filePath: file,
                loader: TextLoader.From(TextAndVersion.Create(
                    SourceText.From(text, Encoding.UTF8),
                    VersionStamp.Create(),
                    file))));
        }

        var workspace = new AdhocWorkspace();
        workspace.AddProject(projectInfo.WithDocuments(documents));
        return workspace;
    }

    private async Task<JsonDocument> RunFallbackMsBuildAsync(IEnumerable<string> arguments)
    {
        // Run under the project's SDK host: the in-process SDK resolver may be precisely
        // why MSBuildWorkspace failed. Match DotnetCliRunner's environment isolation.
        var host = Environment.GetEnvironmentVariable("DOTNET_HOST_PATH");
        if (string.IsNullOrEmpty(host))
        {
            var root = Environment.GetEnvironmentVariable("DOTNET_ROOT");
            host = string.IsNullOrEmpty(root) ? "dotnet" :
                Path.Combine(root, OperatingSystem.IsWindows() ? "dotnet.exe" : "dotnet");
        }
        var startInfo = new ProcessStartInfo(host)
        {
            WorkingDirectory = Path.GetDirectoryName(Path.GetFullPath(_projectPath))!,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true
        };
        startInfo.Environment.Remove("MSBuildSDKsPath");
        startInfo.Environment.Remove("MSBuildExtensionsPath");
        startInfo.Environment.Remove("MSBUILD_EXE_PATH");
        startInfo.ArgumentList.Add("msbuild");
        startInfo.ArgumentList.Add(Path.GetFullPath(_projectPath));
        startInfo.ArgumentList.Add("-nologo");
        foreach (var argument in arguments)
        {
            startInfo.ArgumentList.Add(argument);
        }
        using var process = Process.Start(startInfo)
            ?? throw new InvalidOperationException("Unable to start the .NET SDK host.");
        var outputTask = process.StandardOutput.ReadToEndAsync();
        var errorTask = process.StandardError.ReadToEndAsync();
        await process.WaitForExitAsync();
        var output = await outputTask;
        var error = await errorTask;
        if (process.ExitCode != 0)
        {
            throw new InvalidOperationException($"Unable to resolve compiler inputs for '{_projectPath}'.{Environment.NewLine}{output}{error}");
        }
        return JsonDocument.Parse(output);
    }

    public async Task OpenProjectAsync()
    {
        EnsureInitialized();
        if (string.IsNullOrEmpty(_projectPath))
        {
            return;
        }

        var msBuildWorkspace = await GetMsBuildWorkspaceAsync();
        if (msBuildWorkspace is null)
        {
            return;
        }

        Project? project = msBuildWorkspace.CurrentSolution.GetProject(_projectPath);
        if (project is not null)
        {
            return;
        }

        try
        {
            await msBuildWorkspace.OpenProjectAsync(_projectPath).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex.ToString());
        }
    }

    private void UnloadWorkspace()
    {
        var msBuildWorkspace = _msBuildWorkspace;
        _msBuildWorkspace = null;
        _compilation = null;
        if (msBuildWorkspace is not null)
        {
            _workspaceFailedRegistration?.Dispose();
            _workspaceFailedRegistration = null;
            msBuildWorkspace.CloseSolution();
            msBuildWorkspace.Dispose();
        }

        _fallbackWorkspace?.Dispose();
        _fallbackWorkspace = null;
    }

    public async Task ReloadWorkspaceAsync()
    {
        EnsureInitialized();
        UnloadWorkspace();

        await GetMsBuildWorkspaceAsync(refresh: true);
    }

    //TODO add a debug option to logging.
    private void OnWorkspaceFailed(WorkspaceDiagnosticEventArgs e)
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
            // Model discovery retains its lightweight source-only fallback below.
            // Document modifications instead use the SDK-resolved fallback workspace.
            var msBuildWorkspace = await GetMsBuildWorkspaceAsync();
            var project = msBuildWorkspace?.CurrentSolution?.GetProject(_projectPath);
            if (project is not null)
            {
                _compilation = await project.GetCompilationAsync();
            }

            // Fallback: MSBuildWorkspace can fail to open projects when the SDK resolver
            // cannot evaluate the project (e.g., preview SDK versions like net11.0 in some
            // environments). In that case, parse the .cs source files directly so that
            // model classes and their properties can still be discovered.
            if (_compilation is null)
            {
                _compilation = await GetFallbackCompilationAsync();
            }
        }

        List<ISymbol?>? compilationClassSymbols = _compilation?.SyntaxTrees.SelectMany(tree =>
        {
            var model = _compilation.GetSemanticModel(tree);
            return tree.GetRoot().DescendantNodes().OfType<ClassDeclarationSyntax>()
                .Select(classSyntax => model.GetDeclaredSymbol(classSyntax))
                .Where(classSymbol => classSymbol is not null &&
                                     !classSymbol.MetadataName.StartsWith("<"));    //if the metadata name starts with < it is a compiler generated class
        })
        .Append(_compilation.GetEntryPoint(CancellationToken.None)?.ContainingType)
        .Distinct(SymbolEqualityComparer.Default)
        .ToList();

        compilationClassSymbols?.ForEach(x =>
        {
            if (x is not null)
            {
                classSymbols.Add(x);
            }
        });

        return classSymbols;
    }

    /// <summary>
    /// Fallback compilation built by directly parsing the project's .cs source files.
    /// Used when <see cref="MSBuildWorkspace"/> cannot evaluate the project (e.g., preview
    /// SDK versions). Metadata references are gathered from the currently-loaded assemblies
    /// in <see cref="AppDomain.CurrentDomain"/> so that primitive types such as
    /// <c>int</c> and <c>string</c> resolve correctly for model-class discovery.
    /// </summary>
    private async Task<Compilation?> GetFallbackCompilationAsync()
    {
        var projectDirectory = Path.GetDirectoryName(_projectPath);
        if (string.IsNullOrEmpty(projectDirectory) || !Directory.Exists(projectDirectory))
        {
            return null;
        }

        var sourceFiles = Directory.GetFiles(projectDirectory, "*.cs", SearchOption.AllDirectories)
            .Where(f =>
            {
                var rel = Path.GetRelativePath(projectDirectory, f);
                return !rel.StartsWith("obj" + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase)
                    && !rel.StartsWith("bin" + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase);
            })
            .ToArray();

        if (sourceFiles.Length == 0)
        {
            return null;
        }

        var parseOptions = CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.Latest);
        var syntaxTrees = new List<SyntaxTree>();
        foreach (var sourceFile in sourceFiles)
        {
            try
            {
                var text = await File.ReadAllTextAsync(sourceFile);
                syntaxTrees.Add(CSharpSyntaxTree.ParseText(text, parseOptions, path: sourceFile));
            }
            catch { /* skip files that can't be read */ }
        }

        if (syntaxTrees.Count == 0)
        {
            return null;
        }

        // Use the assemblies already loaded into the current AppDomain as metadata references.
        // This provides resolution for all primitive and framework types present in the
        // running .NET version without requiring a separate NuGet restore step.
        var references = AppDomain.CurrentDomain.GetAssemblies()
            .Where(a => !a.IsDynamic && !string.IsNullOrEmpty(a.Location))
            .Select(a =>
            {
                try { return (MetadataReference)MetadataReference.CreateFromFile(a.Location); }
                catch { return null; }
            })
            .OfType<MetadataReference>();

        return CSharpCompilation.Create(
            assemblyName: "FallbackCompilation",
            syntaxTrees: syntaxTrees,
            references: references,
            options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
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
