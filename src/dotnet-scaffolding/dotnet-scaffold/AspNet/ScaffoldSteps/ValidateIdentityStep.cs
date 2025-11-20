// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.DotNet.Scaffolding.Core.Scaffolders;
using Microsoft.DotNet.Scaffolding.Core.Steps;
using Microsoft.DotNet.Scaffolding.Internal.Services;
using Microsoft.DotNet.Scaffolding.Internal.Telemetry;
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
        var identityModel = await GetIdentityModelAsync(identitySettings);
        if (identityModel is null)
        {
            _logger.LogError("An error occurred.");
            _telemetryService.TrackEvent(new ValidateScaffolderTelemetryEvent(nameof(ValidateIdentityStep), context.Scaffolder.DisplayName, false));
            return false;
        }
        else
        {
            context.Properties.Add(nameof(IdentityModel), identityModel);
            codeModifierProperties.Add(Constants.CodeModifierPropertyConstants.IdentityNamespace, identityModel.IdentityNamespace);
            codeModifierProperties.Add(Constants.CodeModifierPropertyConstants.UserClassNamespace, identityModel.UserClassNamespace);
        }

        //Install packages and add a DbContext (if needed)
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
    /// <param name="settings">The IdentitySettings used to initialize the model.</param>
    /// <returns>A task that represents the asynchronous operation, with a result of the IdentityModel.</returns>
    private async Task<IdentityModel?> GetIdentityModelAsync(IdentitySettings settings)
    {
        var projectInfo = ClassAnalyzers.GetProjectInfo(settings.Project, _logger);
        var projectDirectory = Path.GetDirectoryName(projectInfo.ProjectPath);
        if (projectInfo is null || projectInfo.CodeService is null || string.IsNullOrEmpty(projectDirectory))
        {
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
            identityLayoutNamespace = settings.BlazorScenario ? $"{projectName}.Components.Layout.MainLayout" : string.Empty;
            userClassNamespace = $"{projectName}.Data";
        }

        IdentityModel scaffoldingModel = new()
        {
            ProjectInfo = projectInfo,
            DbContextInfo = dbContextInfo,
            IdentityNamespace = identityNamespace,
            UserClassName = AspNetConstants.Identity.UserClassName,
            UserClassNamespace = userClassNamespace,
            IdentityLayoutNamespace = identityLayoutNamespace,
            BaseOutputPath = projectDirectory,
            Overwrite = settings.Overwrite
        };

        if (scaffoldingModel.ProjectInfo is not null && scaffoldingModel.ProjectInfo.CodeService is not null)
        {
            scaffoldingModel.ProjectInfo.CodeChangeOptions =
            [
                scaffoldingModel.DbContextInfo.EfScenario ? "EfScenario" : string.Empty
            ];
        }

        return scaffoldingModel;
    }
}
