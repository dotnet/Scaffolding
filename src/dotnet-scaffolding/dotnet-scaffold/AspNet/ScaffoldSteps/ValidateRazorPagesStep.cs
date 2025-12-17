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
/// Scaffold step to validate Razor Pages settings and initialize the RazorPageModel for scaffolding.
/// </summary>
internal class ValidateRazorPagesStep : ScaffoldStep
{
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
    /// Name of the model class for the Razor Page.
    /// </summary>
    public string? Model { get; set; }
    /// <summary>
    /// Page type for Razor Pages scaffolding.
    /// </summary>
    public string? Page { get; set; }

    private readonly IFileSystem _fileSystem;
    private readonly ILogger _logger;
    private readonly ITelemetryService _telemetryService;

    /// <summary>
    /// Constructor for ValidateRazorPagesStep.
    /// </summary>
    /// <param name="fileSystem">The file system.</param>
    /// <param name="logger">The logger.</param>
    /// <param name="telemetryService">The telemetry service.</param>
    public ValidateRazorPagesStep(
        IFileSystem fileSystem,
        ILogger<ValidateRazorPagesStep> logger,
        ITelemetryService telemetryService)
    {
        _fileSystem = fileSystem;
        _logger = logger;
        _telemetryService = telemetryService;
    }

    /// <summary>
    /// Executes the step to validate Razor Pages settings and initialize the RazorPageModel.
    /// </summary>
    /// <param name="context">The scaffolder context.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task that represents the asynchronous operation, with a boolean result indicating success or failure.</returns>
    public override async Task<bool> ExecuteAsync(ScaffolderContext context, CancellationToken cancellationToken = default)
    {
        var razorPagesSettings = ValidateRazorPagesSettings();
        var codeModifierProperties = new Dictionary<string, string>();
        if (razorPagesSettings is null)
        {
            _telemetryService.TrackEvent(new ValidateScaffolderTelemetryEvent(nameof(ValidateRazorPagesStep), context.Scaffolder.DisplayName, result: false));
            return false;
        }
        else
        {
            context.Properties.Add(nameof(CrudSettings), razorPagesSettings);
        }

        //initialize RazorPageModel
        _logger.LogInformation("Initializing scaffolding model...");
        var razorPageModel = await GetRazorPageModelAsync(context, razorPagesSettings);
        if (razorPageModel is null)
        {
            _logger.LogError("An error occurred.");
            _telemetryService.TrackEvent(new ValidateScaffolderTelemetryEvent(nameof(ValidateRazorPagesStep), context.Scaffolder.DisplayName, result: false));
            return false;
        }
        else
        {
            context.Properties.Add(nameof(RazorPageModel), razorPageModel);
        }

        //Install packages and add a DbContext (if needed)
        if (razorPageModel.DbContextInfo.EfScenario)
        {
            var dbContextProperties = AspNetDbContextHelper.GetDbContextProperties(razorPagesSettings.Project, razorPageModel.DbContextInfo);
            if (dbContextProperties is not null)
            {
                context.Properties.Add(nameof(DbContextProperties), dbContextProperties);
            }

            var projectBasePath = Path.GetDirectoryName(razorPagesSettings.Project);
            if (!string.IsNullOrEmpty(projectBasePath))
            {
                context.Properties.Add(Constants.StepConstants.BaseProjectPath, projectBasePath);
            }

            codeModifierProperties = AspNetDbContextHelper.GetDbContextCodeModifierProperties(razorPageModel.DbContextInfo);
        }

        context.Properties.Add(Constants.StepConstants.CodeModifierProperties, codeModifierProperties);
        _telemetryService.TrackEvent(new ValidateScaffolderTelemetryEvent(nameof(ValidateRazorPagesStep), context.Scaffolder.DisplayName, result: true));
        return true;
    }

    /// <summary>
    /// Validates the Razor Pages settings provided by the user.
    /// </summary>
    /// <returns>A CrudSettings object with the validated settings, or null if validation fails.</returns>
    private CrudSettings? ValidateRazorPagesSettings()
    {
        if (string.IsNullOrEmpty(Project) || !_fileSystem.FileExists(Project))
        {
            _logger.LogError($"Missing/Invalid {AspNetConstants.CliOptions.ProjectCliOption} option.");
            return null;
        }

        if (string.IsNullOrEmpty(Model))
        {
            _logger.LogError($"Missing/Invalid {AspNetConstants.CliOptions.ModelCliOption} option.");
            return null;
        }

        if (string.IsNullOrEmpty(Page))
        {
            _logger.LogError($"Missing/Invalid {AspNetConstants.CliOptions.PageTypeOption} option.");
            return null;
        }
        else if (!string.IsNullOrEmpty(Page) && !BlazorCrudHelper.CRUDPages.Contains(Page, StringComparer.OrdinalIgnoreCase))
        {
            //if an invalid page name, switch to just "CRUD" and scaffold all pages
            Page = BlazorCrudHelper.CrudPageType;
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

            if (string.IsNullOrEmpty(DatabaseProvider) || !PackageConstants.EfConstants.EfPackagesDict.ContainsKey(DatabaseProvider))
            {
                DatabaseProvider = PackageConstants.EfConstants.SqlServer;
            }
        }

        return new CrudSettings
        {
            Model = Model,
            Project = Project,
            Page = Page,
            DataContext = DataContext,
            DatabaseProvider = DatabaseProvider,
            Prerelease = Prerelease
        };
    }

    /// <summary>
    /// Initializes and returns the RazorPageModel for scaffolding.
    /// </summary>
    /// <param name="context">The scaffolder context.</param>
    /// <param name="settings">The validated CRUD settings.</param>
    /// <returns>A task that represents the asynchronous operation, with the RazorPageModel as the result.</returns>
    private async Task<RazorPageModel?> GetRazorPageModelAsync(ScaffolderContext context, CrudSettings settings)
    {
        ProjectInfo projectInfo = ClassAnalyzers.GetProjectInfo(settings.Project, _logger);
        context.SetSpecifiedTargetFramework(projectInfo.LowestSupportedTargetFramework);
        var projectDirectory = Path.GetDirectoryName(projectInfo.ProjectPath);
        if (projectInfo is null || projectInfo.CodeService is null || string.IsNullOrEmpty(projectDirectory))
        {
            return null;
        }

        //find and set --model class properties
        ModelInfo? modelInfo = null;
        var allClasses = await projectInfo.CodeService.GetAllClassSymbolsAsync();
        var modelClassSymbol = allClasses.FirstOrDefault(x => x.Name.Equals(settings.Model, StringComparison.OrdinalIgnoreCase));
        if (string.IsNullOrEmpty(settings.Model) || modelClassSymbol is null)
        {
            _logger.LogError($"Invalid {AspNetConstants.CliOptions.ModelCliOption} '{settings.Model}'") ;
            return null;
        }
        else
        {
            modelInfo = ClassAnalyzers.GetModelClassInfo(modelClassSymbol);
        }

        var validateModelInfoResult = ClassAnalyzers.ValidateModelForCrudScaffolders(modelInfo, _logger);
        if (!validateModelInfoResult)
        {
            _logger.LogError($"Invalid {AspNetConstants.CliOptions.ModelCliOption} '{settings.Model}'");
            return null;
        }

        //find DbContext info or create properties for a new one.
        var dbContextClassName = settings.DataContext;
        DbContextInfo dbContextInfo = new();

        if (!string.IsNullOrEmpty(dbContextClassName) && !string.IsNullOrEmpty(settings.DatabaseProvider))
        {
            var dbContextClassSymbol = allClasses.FirstOrDefault(x => x.Name.Equals(dbContextClassName, StringComparison.OrdinalIgnoreCase));
            dbContextInfo = ClassAnalyzers.GetDbContextInfo(settings.Project, dbContextClassSymbol, dbContextClassName, settings.DatabaseProvider, modelInfo);
            dbContextInfo.EfScenario = true;
        }

        string razorPageNamespace = string.Empty;
        var projectName = Path.GetFileNameWithoutExtension(settings.Project);
        if (!string.IsNullOrEmpty(projectName))
        {
            razorPageNamespace = $"{projectName}.Pages.{modelInfo.ModelTypeName}Pages";
        }

        RazorPageModel scaffoldingModel = new()
        {
            ProjectInfo = projectInfo,
            ModelInfo = modelInfo,
            DbContextInfo = dbContextInfo,
            PageType = settings.Page,
            RazorPageNamespace = razorPageNamespace
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
