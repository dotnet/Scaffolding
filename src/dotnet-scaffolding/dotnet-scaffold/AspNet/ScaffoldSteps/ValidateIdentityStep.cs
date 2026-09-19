// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.DotNet.Scaffolding.Core.Model;
using Microsoft.DotNet.Scaffolding.Core.Scaffolders;
using Microsoft.DotNet.Scaffolding.Core.Steps;
using Microsoft.DotNet.Scaffolding.Internal;
using Microsoft.DotNet.Scaffolding.Internal.Services;
using Microsoft.DotNet.Scaffolding.Internal.Telemetry;
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
/// Validates and normalizes Identity settings, analyzes the application, and prepares the model,
/// DbContext configuration, and code-modification inputs consumed by later scaffolding steps.
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
    /// Validates settings and prepares the inputs consumed by later Identity scaffolding steps.
    /// </summary>
    /// <param name="context">Scaffolder context.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Task that represents the asynchronous operation, with a boolean result indicating success or failure.</returns>
    public override async Task<bool> ExecuteAsync(ScaffolderContext context, CancellationToken cancellationToken = default)
    {
        var identitySettings = ValidateIdentitySettings();
        if (identitySettings is null)
        {
            _telemetryService.TrackEvent(new ValidateScaffolderTelemetryEvent(nameof(ValidateIdentityStep), context.Scaffolder.DisplayName, false));
            return false;
        }

        _logger.LogInformation("Initializing scaffolding model...");
        var identityModel = await GetIdentityModelAsync(identitySettings);
        if (identityModel is null)
        {
            _logger.LogError("An error occurred.");
            _telemetryService.TrackEvent(new ValidateScaffolderTelemetryEvent(nameof(ValidateIdentityStep), context.Scaffolder.DisplayName, false));
            return false;
        }

        var codeModifierProperties = PrepareCodeModificationInputs(identitySettings, identityModel);
        if (codeModifierProperties is null)
        {
            _logger.LogError("An error occurred.");
            _telemetryService.TrackEvent(new ValidateScaffolderTelemetryEvent(nameof(ValidateIdentityStep), context.Scaffolder.DisplayName, false));
            return false;
        }

        context.Properties.Add(nameof(IdentitySettings), identitySettings);
        context.Properties.Add(nameof(IdentityModel), identityModel);
        context.SetSpecifiedTargetFramework(identityModel.ProjectInfo.LowestSupportedTargetFramework);

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
    /// Validates required options and applies the DbContext name and database provider defaults.
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
    /// Discovers project and DbContext information and infers the namespaces and layout for generated files.
    /// </summary>
    /// <param name="settings">The IdentitySettings used to initialize the model.</param>
    /// <returns>The prepared model, or null if project analysis is unavailable.</returns>
    private async Task<IdentityModel?> GetIdentityModelAsync(IdentitySettings settings)
    {
        ProjectInfo projectInfo = ClassAnalyzers.GetProjectInfo(settings.Project, _logger);
        var projectDirectory = Path.GetDirectoryName(projectInfo.ProjectPath);
        if (projectInfo is null || projectInfo.CodeService is null || string.IsNullOrEmpty(projectDirectory))
        {
            return null;
        }

        // Failed framework evaluation must not silently bypass the Net11 preparation below.
        if (settings.BlazorScenario && projectInfo.LowestSupportedTargetFramework is null)
        {
            _logger.LogError(
                $"Unable to determine a supported target framework for '{settings.Project}'. Ensure the project's SDK and imports are available and it targets .NET 8 or later. Run 'dotnet msbuild \"{settings.Project}\" -getProperty:TargetFramework,TargetFrameworks' for evaluation diagnostics.");
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
        return new IdentityModel
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
    }

    /// <summary>
    /// Prepares JSON code-change flags and their placeholder substitutions, including Blazor application analysis.
    /// </summary>
    /// <returns>The substitutions, or null if a required Blazor WebAssembly client cannot be resolved.</returns>
    private Dictionary<string, string>? PrepareCodeModificationInputs(IdentitySettings settings, IdentityModel identityModel)
    {
        var codeChangeOptions = new List<string>();
        var codeModifierProperties = new Dictionary<string, string>
        {
            [Constants.CodeModifierPropertyConstants.IdentityNamespace] = identityModel.IdentityNamespace,
            [Constants.CodeModifierPropertyConstants.UserClassNamespace] = identityModel.UserClassNamespace
        };
        if (identityModel.DbContextInfo.EfScenario)
        {
            codeChangeOptions.Add("EfScenario");
        }

        if (settings.BlazorScenario && identityModel.ProjectInfo.LowestSupportedTargetFramework == TargetFramework.Net11 &&
            !TryPrepareBlazorIdentityInputs(identityModel, codeChangeOptions, codeModifierProperties))
        {
            return null;
        }

        identityModel.ProjectInfo.CodeChangeOptions = codeChangeOptions;
        return codeModifierProperties;
    }

    /// <summary>
    /// Detects interactivity and the global render mode, resolves the required client, then derives generation inputs.
    /// </summary>
    /// <returns>False if WebAssembly support is detected but exactly one referenced client cannot be resolved.</returns>
    private bool TryPrepareBlazorIdentityInputs(
        IdentityModel identityModel,
        List<string> codeChangeOptions,
        Dictionary<string, string> codeModifierProperties)
    {
        var projectDirectory = identityModel.BaseOutputPath;
        var programPath = Path.Combine(projectDirectory, "Program.cs");
        if (!_fileSystem.FileExists(programPath))
        {
            return true;
        }

        var programContent = _fileSystem.ReadAllText(programPath);
        var usesInteractiveServer = programContent.Contains(
            BlazorCrudHelper.AddInteractiveServerComponentsMethod,
            StringComparison.Ordinal);
        var usesInteractiveWebAssembly = programContent.Contains(
            BlazorCrudHelper.AddInteractiveWebAssemblyComponentsMethod,
            StringComparison.Ordinal);

        if (usesInteractiveWebAssembly)
        {
            var projectPath = identityModel.ProjectInfo.ProjectPath;
            if (string.IsNullOrEmpty(projectPath))
            {
                _logger.LogError("Unable to resolve the Blazor WebAssembly client project because the server project path is unavailable.");
                return false;
            }

            var clientProjectPaths = GetReferencedBlazorWebAssemblyProjects(projectPath);
            if (clientProjectPaths is null)
            {
                return false;
            }

            if (clientProjectPaths.Count != 1)
            {
                var detail = clientProjectPaths.Count == 0
                    ? "No referenced project using the Microsoft.NET.Sdk.BlazorWebAssembly SDK was found."
                    : $"Multiple referenced projects use the Microsoft.NET.Sdk.BlazorWebAssembly SDK: {string.Join(", ", clientProjectPaths)}.";
                _logger.LogError(
                    $"Unable to resolve the Blazor WebAssembly client project for '{identityModel.ProjectInfo.ProjectPath}'. {detail} Ensure the server project has exactly one ProjectReference to its Blazor WebAssembly client.");
                return false;
            }

            identityModel.BlazorWebAssemblyClientProjectPath = clientProjectPaths[0];
        }

        var appPath = Path.Combine(projectDirectory, "Components", "App.razor");
        if (_fileSystem.FileExists(appPath))
        {
            var appContent = _fileSystem.ReadAllText(appPath);
            identityModel.BlazorRenderMode = new[] { "InteractiveAuto", "InteractiveServer", "InteractiveWebAssembly" }
                .FirstOrDefault(renderMode => appContent.Contains($"@rendermode=\"{renderMode}\"", StringComparison.Ordinal));
        }

        // Keep JSON flags and substitutions together: client changes require a resolved client,
        // and global routing changes require the same render mode used by the generated components.
        codeChangeOptions.Add(usesInteractiveServer ? "InteractiveServer" : "NonInteractiveServer");
        if (usesInteractiveWebAssembly)
        {
            codeChangeOptions.Add("InteractiveWebAssembly");
            codeModifierProperties.Add(
                "$(BlazorWebAssemblyClientNamespace)",
                Path.GetFileNameWithoutExtension(identityModel.BlazorWebAssemblyClientProjectPath!));
        }

        if (identityModel.BlazorRenderMode is not null)
        {
            codeChangeOptions.Add("GlobalInteractive");
            codeModifierProperties.Add($"$({nameof(IdentityModel.BlazorRenderMode)})", identityModel.BlazorRenderMode);
        }

        return true;
    }

    private List<string>? GetReferencedBlazorWebAssemblyProjects(string projectPath)
    {
        var projectService = new MSBuildProjectService(projectPath);
        if (!projectService.TryGetProjectReferences(out var references, out var error))
        {
            _logger.LogError($"Unable to evaluate project references for '{projectPath}'. {error} Ensure the project's SDK and imports are available.");
            return null;
        }

        return references
            .Where(_fileSystem.FileExists)
            .Where(reference => _fileSystem.ReadAllText(reference).Contains(
                "Sdk=\"Microsoft.NET.Sdk.BlazorWebAssembly\"",
                StringComparison.Ordinal))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
    }
}
