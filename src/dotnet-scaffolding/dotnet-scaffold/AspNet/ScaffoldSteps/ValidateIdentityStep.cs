// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.DotNet.Scaffolding.Core.Model;
using Microsoft.DotNet.Scaffolding.Core.Scaffolders;
using Microsoft.DotNet.Scaffolding.Core.Steps;
using Microsoft.DotNet.Scaffolding.Internal;
using Microsoft.DotNet.Scaffolding.Internal.CliHelpers;
using Microsoft.DotNet.Scaffolding.Internal.Services;
using Microsoft.DotNet.Scaffolding.Internal.Telemetry;
using Microsoft.DotNet.Scaffolding.Roslyn;
using Microsoft.DotNet.Scaffolding.Roslyn.Services;
using Microsoft.DotNet.Scaffolding.TextTemplating.DbContext;
using Microsoft.DotNet.Tools.Scaffold.AspNet.Common;
using Microsoft.DotNet.Tools.Scaffold.AspNet.Helpers;
using Microsoft.DotNet.Tools.Scaffold.AspNet.Models;
using Microsoft.DotNet.Tools.Scaffold.AspNet.ScaffoldSteps.Settings;
using Microsoft.DotNet.Tools.Scaffold.AspNet.Telemetry;
using Microsoft.Extensions.Logging;
using AspNetConstants = Microsoft.DotNet.Tools.Scaffold.AspNet.Common.Constants;
using Constants = Microsoft.DotNet.Scaffolding.Internal.Constants;

namespace Microsoft.DotNet.Tools.Scaffold.AspNet.ScaffoldSteps;

/// <summary>
/// Scaffold step to validate Identity settings and initialize the IdentityModel for scaffolding.
/// </summary>
//TODO: pull all the duplicate logic from all these 'Validation' ScaffolderSteps into a common one.
internal class ValidateIdentityStep : ScaffoldStep
{
    private readonly IFileSystem _fileSystem;
    private readonly ILogger _logger;
    private readonly ITelemetryService _telemetryService;

    /// <summary>
    /// Indicates whether to overwrite existing files.
    /// </summary>
    public bool Overwrite { get; set; }
    /// <summary>
    /// Indicates whether the scenario is for Blazor.
    /// </summary>
    public bool BlazorScenario { get; set; }
    /// <summary>
    /// Path to the project file.
    /// </summary>
    public string? Project { get; set; }
    /// <summary>
    /// Indicates if prerelease packages should be used.
    /// </summary>
    public bool Prerelease { get; set; }
    /// <summary>
    /// Database provider for the DbContext.
    /// </summary>
    public string? DatabaseProvider { get; set; }
    /// <summary>
    /// Name of the DbContext class.
    /// </summary>
    public string? DataContext { get; set; }

    /// <summary>
    /// Constructor for ValidateIdentityStep.
    /// </summary>
    /// <param name="fileSystem">File system service.</param>
    /// <param name="logger">Logger.</param>
    /// <param name="telemetryService">Telemetry service.</param>
    public ValidateIdentityStep(
        IFileSystem fileSystem,
        ILogger<ValidateIdentityStep> logger,
        ITelemetryService telemetryService)
    {
        _fileSystem = fileSystem;
        _logger = logger;
        _telemetryService = telemetryService;
    }

    /// <summary>
    /// Executes the step to validate Identity settings and initialize the IdentityModel.
    /// </summary>
    /// <param name="context">Scaffolder context.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Task that represents the asynchronous operation, with a boolean result indicating success or failure.</returns>
    public override async Task<bool> ExecuteAsync(ScaffolderContext context, CancellationToken cancellationToken = default)
    {
        var identitySettings = ValidateIdentitySettings();
        var codeModifierProperties = new Dictionary<string, string>();
        if (identitySettings is null)
        {
            _telemetryService.TrackEvent(new ValidateScaffolderTelemetryEvent(nameof(ValidateIdentityStep), context.Scaffolder.DisplayName, false));
            return false;
        }
        else
        {
            context.Properties.Add(nameof(IdentitySettings), identitySettings);
        }

        //initialize IdentityModel
        _logger.LogInformation("Initializing scaffolding model...");
        var identityModel = await GetIdentityModelAsync(context, identitySettings);
        if (identityModel is null)
        {
            _telemetryService.TrackEvent(new ValidateScaffolderTelemetryEvent(nameof(ValidateIdentityStep), context.Scaffolder.DisplayName, false));
            return false;
        }
        else
        {
            context.Properties.Add(nameof(IdentityModel), identityModel);
            codeModifierProperties.Add(Constants.CodeModifierPropertyConstants.IdentityNamespace, identityModel.IdentityNamespace);
            codeModifierProperties.Add(Constants.CodeModifierPropertyConstants.UserClassNamespace, identityModel.UserClassNamespace);
        }

        if (!await PrepareCodeModificationInputsAsync(identitySettings, identityModel, codeModifierProperties))
        {
            _telemetryService.TrackEvent(new ValidateScaffolderTelemetryEvent(nameof(ValidateIdentityStep), context.Scaffolder.DisplayName, false));
            return false;
        }

        // Prepare configuration only; later steps install packages and create the DbContext.
        if (identityModel.DbContextInfo.EfScenario)
        {
            var dbContextProperties = AspNetDbContextHelper.GetDbContextProperties(identitySettings.Project, identityModel.DbContextInfo);
            if (dbContextProperties is not null)
            {
                dbContextProperties.IsIdentityDbContext = true;
                dbContextProperties.FullIdentityUserName = $"{identityModel.UserClassNamespace}.{identityModel.UserClassName}";
                context.Properties.Add(nameof(DbContextProperties), dbContextProperties);
            }

            var projectBasePath = Path.GetDirectoryName(identitySettings.Project);
            if (!string.IsNullOrEmpty(projectBasePath))
            {
                context.Properties.Add(Constants.StepConstants.BaseProjectPath, projectBasePath);
            }

            var dbCodeModifierProperties = AspNetDbContextHelper.GetDbContextCodeModifierProperties(identityModel.DbContextInfo);
            foreach (var kvp in dbCodeModifierProperties)
            {
                codeModifierProperties.TryAdd(kvp.Key, kvp.Value);
            }

            codeModifierProperties.TryAdd(Constants.CodeModifierPropertyConstants.UserClassName, identityModel.UserClassName);
        }

        context.Properties.Add(Constants.StepConstants.CodeModifierProperties, codeModifierProperties);
        _telemetryService.TrackEvent(new ValidateScaffolderTelemetryEvent(nameof(ValidateIdentityStep), context.Scaffolder.DisplayName, true));
        return true;
    }

    /// <summary>
    /// Validates the Identity settings provided by the user.
    /// </summary>
    /// <returns>Returns the validated IdentitySettings object, or null if validation failed.</returns>
    private IdentitySettings? ValidateIdentitySettings()
    {
        if (string.IsNullOrEmpty(Project) || !_fileSystem.FileExists(Project))
        {
            _logger.LogError($"Missing/Invalid {AspNetConstants.CliOptions.ProjectCliOption} option.");
            return null;
        }

        if (string.IsNullOrEmpty(DataContext))
        {
            _logger.LogError($"Missing/Invalid {AspNetConstants.CliOptions.DataContextOption} option.");
            return null;
        }
        else
        {
            if (!SyntaxFacts.IsValidIdentifier(DataContext) || DataContext.Equals("DbContext", StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogInformation($"Invalid {AspNetConstants.CliOptions.DataContextOption} option");
                _logger.LogInformation($"Using default '{AspNetConstants.NewDbContext}'");
                DataContext = AspNetConstants.NewDbContext;
            }

            if (string.IsNullOrEmpty(DatabaseProvider) || !PackageConstants.EfConstants.IdentityEfPackagesDict.ContainsKey(DatabaseProvider))
            {
                DatabaseProvider = PackageConstants.EfConstants.SqlServer;
            }
        }

        return new IdentitySettings
        {
            Project = Project,
            DataContext = DataContext,
            DatabaseProvider = DatabaseProvider,
            Prerelease = Prerelease,
            Overwrite = Overwrite,
            BlazorScenario = BlazorScenario
        };
    }

    /// <summary>
    /// Initializes and returns the IdentityModel for scaffolding.
    /// </summary>
    /// <param name="context">The ScaffolderContext for the current operation.</param>
    /// <param name="settings">The IdentitySettings used to initialize the model.</param>
    /// <returns>A task that represents the asynchronous operation, with a result of the IdentityModel.</returns>
    private async Task<IdentityModel?> GetIdentityModelAsync(ScaffolderContext context, IdentitySettings settings)
    {
        var projectPath = Path.GetFullPath(settings.Project);
        ProjectInfo projectInfo = ClassAnalyzers.GetProjectInfo(projectPath, _logger);
        context.SetSpecifiedTargetFramework(projectInfo.LowestSupportedTargetFramework);
        var projectDirectory = Path.GetDirectoryName(projectInfo.ProjectPath);
        if (projectInfo is null || projectInfo.CodeService is null || string.IsNullOrEmpty(projectDirectory))
        {
            _logger.LogError($"Unable to initialize Identity scaffolding for '{projectPath}': project information, code analysis, or the project directory is unavailable.");
            return null;
        }

        if (settings.BlazorScenario && projectInfo.LowestSupportedTargetFramework is null)
        {
            _logger.LogError(
                $"Unable to determine a supported target framework for '{settings.Project}'. Ensure the required .NET SDK and project imports are available and the project targets a framework supported by this version of dotnet scaffold. Run 'dotnet msbuild \"{settings.Project}\" -getProperty:TargetFramework,TargetFrameworks' for evaluation diagnostics.");
            return null;
        }

        // Restore existing dependencies before CodeService first loads the workspace for semantic analysis.
        _logger.LogInformation("Restoring project dependencies for Identity analysis...");
        var runner = DotnetCliRunner.CreateDotNet("restore", [projectPath, "--disable-build-servers"]);
        runner._psi.WorkingDirectory = projectDirectory;
        if (runner.ExecuteAndCaptureOutput(out var output, out var error) != 0)
        {
            _logger.LogError(
                $"Unable to restore '{settings.Project}' for Identity analysis. Run 'dotnet restore \"{projectPath}\"' and resolve the errors before scaffolding.\n{output}\n{error}");
            return null;
        }

        var allClasses = await projectInfo.CodeService.GetAllClassSymbolsAsync();
        //find DbContext info or create properties for a new one.
        var dbContextClassName = settings.DataContext;
        DbContextInfo dbContextInfo = new();

        if (!string.IsNullOrEmpty(dbContextClassName) && !string.IsNullOrEmpty(settings.DatabaseProvider))
        {
            var dbContextClassSymbol = allClasses.FirstOrDefault(x => x.Name.Equals(dbContextClassName, StringComparison.OrdinalIgnoreCase));
            dbContextInfo = ClassAnalyzers.GetIdentityDbContextInfo(settings.Project, dbContextClassSymbol, dbContextClassName, settings.DatabaseProvider);
            dbContextInfo.EfScenario = true;
        }

        string identityNamespace = string.Empty;
        string userClassNamespace = string.Empty;
        string identityLayoutNamespace = string.Empty;
        var projectName = Path.GetFileNameWithoutExtension(settings.Project);
        if (!string.IsNullOrEmpty(projectName))
        {
            identityNamespace = settings.BlazorScenario ? $"{projectName}.Components.Account" : $"{projectName}.Areas.Identity";
            if (settings.BlazorScenario)
            {
                // For WASM/Auto Global Blazor projects, MainLayout lives in the client project (e.g. BlazorApp1.Client).
                // For Blazor Server projects, it lives directly under Components/Layout/ in the server project.
                var mainLayoutInServerProject = Path.Combine(projectDirectory, "Components", "Layout", "MainLayout.razor");
                identityLayoutNamespace = _fileSystem.FileExists(mainLayoutInServerProject)
                    ? $"{projectName}.Components.Layout.MainLayout"
                    : $"{projectName}.Client.Layout.MainLayout";
            }

            userClassNamespace = $"{projectName}.Data";
        }

        bool isRazorPages = Directory.Exists(Path.Combine(projectDirectory, "Pages"));
        IdentityModel scaffoldingModel = new()
        {
            ProjectInfo = projectInfo,
            DbContextInfo = dbContextInfo,
            IdentityNamespace = identityNamespace,
            UserClassName = AspNetConstants.Identity.UserClassName,
            UserClassNamespace = userClassNamespace,
            IdentityLayoutNamespace = identityLayoutNamespace,
            BaseOutputPath = projectDirectory,
            Overwrite = settings.Overwrite,
            IsRazorPages = isRazorPages
        };

        return scaffoldingModel;
    }

    /// <summary>
    /// Prepares JSON code-change flags and their placeholder substitutions, including Blazor application analysis.
    /// </summary>
    /// <returns>False if Blazor analysis or required client resolution fails.</returns>
    private async Task<bool> PrepareCodeModificationInputsAsync(
        IdentitySettings settings,
        IdentityModel identityModel,
        Dictionary<string, string> codeModifierProperties)
    {
        var codeChangeOptions = new List<string>();
        if (identityModel.DbContextInfo.EfScenario)
        {
            codeChangeOptions.Add("EfScenario");
        }

        if (settings.BlazorScenario)
        {
            if (!await TryAnalyzeBlazorIdentityAsync(identityModel))
            {
                return false;
            }

            if (BlazorIdentityHelper.UsesInteractivityAwareTemplates(identityModel.ProjectInfo.LowestSupportedTargetFramework))
            {
                codeChangeOptions.Add(identityModel.UsesInteractiveServer ? "InteractiveServer" : "NonInteractiveServer");
                if (identityModel.UsesInteractiveWebAssembly)
                {
                    codeChangeOptions.Add("InteractiveWebAssembly");
                    codeModifierProperties.Add("$(BlazorWebAssemblyClientNamespace)", identityModel.BlazorWebAssemblyClientNamespace!);
                }

                if (identityModel.BlazorRenderMode is not null)
                {
                    codeChangeOptions.Add("GlobalInteractive");
                    codeModifierProperties.Add($"$({nameof(IdentityModel.BlazorRenderMode)})", identityModel.BlazorRenderMode);
                }
            }
        }

        identityModel.ProjectInfo.CodeChangeOptions = codeChangeOptions;
        return true;
    }

    /// <summary>
    /// Detects interactivity and the global render mode and resolves the required client.
    /// </summary>
    /// <returns>False if semantic analysis is unavailable or a required WebAssembly client cannot be resolved.</returns>
    private async Task<bool> TryAnalyzeBlazorIdentityAsync(IdentityModel identityModel)
    {
        var projectDirectory = identityModel.BaseOutputPath;
        var programPath = Path.Combine(projectDirectory, "Program.cs");
        if (!_fileSystem.FileExists(programPath))
        {
            return true;
        }

        var interactivity = await GetBlazorInteractivityAsync(identityModel.ProjectInfo, programPath);
        if (interactivity is null)
        {
            return false;
        }

        identityModel.UsesInteractiveServer = interactivity.Value.UsesInteractiveServer;
        identityModel.UsesInteractiveWebAssembly = interactivity.Value.UsesInteractiveWebAssembly;
        if (identityModel.UsesInteractiveWebAssembly)
        {
            var client = GetBlazorWebAssemblyClient(identityModel.ProjectInfo.ProjectPath);
            if (client is null)
            {
                return false;
            }

            identityModel.BlazorWebAssemblyClientProjectPath = client.Value.ProjectPath;
            identityModel.BlazorWebAssemblyClientNamespace = client.Value.RootNamespace;
        }

        var appPath = Path.Combine(projectDirectory, "Components", "App.razor");
        if (_fileSystem.FileExists(appPath))
        {
            var appContent = _fileSystem.ReadAllText(appPath);
            identityModel.BlazorRenderMode = new[] { "InteractiveAuto", "InteractiveServer", "InteractiveWebAssembly" }
                .FirstOrDefault(renderMode => appContent.Contains($"@rendermode=\"{renderMode}\"", StringComparison.Ordinal));
        }

        return true;
    }

    private async Task<(bool UsesInteractiveServer, bool UsesInteractiveWebAssembly)?> GetBlazorInteractivityAsync(
        ProjectInfo projectInfo, string programPath)
    {
        var programDocument = await projectInfo.CodeService!.GetDocumentAsync("Program.cs");
        var semanticModel = programDocument is null ? null : await programDocument.GetSemanticModelAsync();
        var programRoot = programDocument is null ? null : await programDocument.GetSyntaxRootAsync();
        if (programDocument is null || semanticModel is null || programRoot is null ||
            semanticModel.Compilation.GetTypeByMetadataName(BlazorCrudHelper.IRazorComponentsBuilderType) is null)
        {
            _logger.LogError(
                $"Unable to analyze Blazor registrations in '{programPath}'. Ensure the project's SDK and references are available and 'dotnet restore' succeeds.");
            return null;
        }

        // An unresolved registration is not an absent registration. Other errors (such as unavailable
        // generated Razor component types) need not prevent analysis of these service registrations.
        var unresolvedRegistration = programRoot.DescendantNodes().OfType<SimpleNameSyntax>()
            .FirstOrDefault(name =>
                name.Identifier.ValueText is BlazorCrudHelper.AddInteractiveServerComponentsMethod or BlazorCrudHelper.AddInteractiveWebAssemblyComponentsMethod &&
                (name.Parent is InvocationExpressionSyntax ||
                 name.Parent is MemberAccessExpressionSyntax { Parent: InvocationExpressionSyntax } ||
                 name.Parent is MemberBindingExpressionSyntax { Parent: InvocationExpressionSyntax }) &&
                semanticModel.GetSymbolInfo(name).Symbol is null);
        if (unresolvedRegistration is not null)
        {
            _logger.LogError(
                $"Unable to resolve Blazor registration '{unresolvedRegistration}' in '{programPath}'. Check the registration's imports and package references, then run 'dotnet build \"{projectInfo.ProjectPath}\"' for diagnostics.");
            return null;
        }

        var usesInteractiveServer = await RoslynUtilities.CheckDocumentForMethodInvocationAsync(
            programDocument, BlazorCrudHelper.AddInteractiveServerComponentsMethod, BlazorCrudHelper.IRazorComponentsBuilderType);
        var usesInteractiveWebAssembly = await RoslynUtilities.CheckDocumentForMethodInvocationAsync(
            programDocument, BlazorCrudHelper.AddInteractiveWebAssemblyComponentsMethod, BlazorCrudHelper.IRazorComponentsBuilderType);

        return (usesInteractiveServer, usesInteractiveWebAssembly);
    }

    private (string ProjectPath, string RootNamespace)? GetBlazorWebAssemblyClient(string? projectPath)
    {
        if (string.IsNullOrEmpty(projectPath))
        {
            _logger.LogError("Unable to resolve the Blazor WebAssembly client project because the server project path is unavailable.");
            return null;
        }

        var projectService = new MSBuildProjectService(projectPath);
        if (!projectService.TryGetProjectReferences(out var references, out var error))
        {
            _logger.LogError($"Unable to evaluate project references for '{projectPath}'. {error} Ensure the project's SDK and imports are available.");
            return null;
        }

        var clients = new List<(string ProjectPath, string RootNamespace)>();
        foreach (var reference in references.Where(_fileSystem.FileExists).Distinct(StringComparer.OrdinalIgnoreCase))
        {
            var clientProjectService = new MSBuildProjectService(reference);
            if (!clientProjectService.TryGetEvaluatedProperties(
                ["UsingMicrosoftNETSdkBlazorWebAssembly", "RootNamespace"],
                out var properties,
                out error))
            {
                _logger.LogError(
                    $"Unable to evaluate referenced project '{reference}' while resolving the Blazor WebAssembly client for '{projectPath}'. {error} Ensure the referenced project's SDK and imports are available.");
                return null;
            }

            if (!string.Equals(properties["UsingMicrosoftNETSdkBlazorWebAssembly"], "true", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var rootNamespace = properties["RootNamespace"];
            if (string.IsNullOrWhiteSpace(rootNamespace))
            {
                _logger.LogError(
                    $"Unable to determine the evaluated RootNamespace for Blazor WebAssembly client project '{reference}'. Set RootNamespace or correct the project evaluation before scaffolding.");
                return null;
            }

            clients.Add((reference, rootNamespace));
        }

        if (clients.Count != 1)
        {
            var detail = clients.Count == 0
                ? "No referenced project using the Microsoft.NET.Sdk.BlazorWebAssembly SDK was found."
                : $"Multiple referenced projects use the Microsoft.NET.Sdk.BlazorWebAssembly SDK: {string.Join(", ", clients.Select(client => client.ProjectPath))}.";
            _logger.LogError(
                $"Unable to resolve the Blazor WebAssembly client project for '{projectPath}'. {detail} Ensure the server project has exactly one ProjectReference to its Blazor WebAssembly client.");
            return null;
        }

        return clients[0];
    }
}
