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
/// Scaffold step to validate EF Controller settings and initialize the EfControllerModel for scaffolding.
/// </summary>
internal class ValidateEfControllerStep : ScaffoldStep
{
    private readonly IFileSystem _fileSystem;
    private readonly ILogger _logger;
    private readonly ITelemetryService _telemetryService;

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
    /// Name of the model class for the controller.
    /// </summary>
    public string? Model { get; set; }
    /// <summary>
    /// Unqualified C# controller name, optionally followed by .cs. Directory paths are not supported.
    /// </summary>
    public string? ControllerName { get; set; }
    /// <summary>
    /// Type of the controller (API or MVC).
    /// </summary>
    public string? ControllerType { get; set; }

    /// <summary>
    /// Constructor for ValidateEfControllerStep.
    /// </summary>
    /// <param name="fileSystem">File system interface.</param>
    /// <param name="logger">Logger instance.</param>
    /// <param name="telemetryService">Telemetry service instance.</param>
    public ValidateEfControllerStep(
        IFileSystem fileSystem,
        ILogger<ValidateEfControllerStep> logger,
        ITelemetryService telemetryService)
    {
        _fileSystem = fileSystem;
        _logger = logger;
        _telemetryService = telemetryService;
    }

    /// <summary>
    /// Executes the step to validate EF Controller settings and initialize the EfControllerModel.
    /// </summary>
    /// <param name="context">Scaffolder context.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Task indicating the completion of the operation.</returns>
    public override async Task<bool> ExecuteAsync(ScaffolderContext context, CancellationToken cancellationToken = default)
    {
        var efControllerSettings = ValidateEfControllerSettings(context.GetOptionResult<bool>(AspNetConstants.CliOptions.ViewsOption));
        var codeModifierProperties = new Dictionary<string, string>();
        if (efControllerSettings is null)
        {
            _telemetryService.TrackEvent(new ValidateScaffolderTelemetryEvent(nameof(ValidateEfControllerStep), context.Scaffolder.DisplayName, result: false));
            return false;
        }
        else
        {
            context.Properties.Add(nameof(EfControllerSettings), efControllerSettings);
        }

        //initialize CrudControllerModel
        _logger.LogInformation("Initializing scaffolding model...");
        var efControllerModel = await GetEfControllerModelAsync(context, efControllerSettings);
        if (efControllerModel is null)
        {
            _logger.LogError("An error occurred.");
            _telemetryService.TrackEvent(new ValidateScaffolderTelemetryEvent(nameof(ValidateEfControllerStep), context.Scaffolder.DisplayName, result: false));
            return false;
        }
        else
        {
            context.Properties.Add(nameof(EfControllerModel), efControllerModel);
        }

        //Install packages and add a DbContext (if needed)
        if (efControllerModel.DbContextInfo.EfScenario)
        {
            var dbContextProperties = AspNetDbContextHelper.GetDbContextProperties(efControllerSettings.Project, efControllerModel.DbContextInfo);
            if (dbContextProperties is not null)
            {
                context.Properties.Add(nameof(DbContextProperties), dbContextProperties);
            }

            var projectBasePath = Path.GetDirectoryName(efControllerSettings.Project);
            if (!string.IsNullOrEmpty(projectBasePath))
            {
                context.Properties.Add(Constants.StepConstants.BaseProjectPath, projectBasePath);
            }

            codeModifierProperties = AspNetDbContextHelper.GetDbContextCodeModifierProperties(efControllerModel.DbContextInfo);
        }

        context.Properties.Add(Constants.StepConstants.CodeModifierProperties, codeModifierProperties);
        _telemetryService.TrackEvent(new ValidateScaffolderTelemetryEvent(nameof(ValidateEfControllerStep), context.Scaffolder.DisplayName, result: true));
        return true;
    }

    /// <summary>
    /// Validates the EF Controller settings provided by the user.
    /// </summary>
    /// <param name="addViews">Whether controller-specific view folders will be generated.</param>
    /// <returns>Validated EF Controller settings.</returns>
    internal EfControllerSettings? ValidateEfControllerSettings(bool addViews)
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

        if (string.IsNullOrEmpty(ControllerName))
        {
            _logger.LogError($"Missing/Invalid {AspNetConstants.CliOptions.ControllerNameOption} option.");
            return null;
        }
        string controllerName = ControllerName.EndsWith(AspNetConstants.CSharpExtension, StringComparison.OrdinalIgnoreCase)
            ? ControllerName[..^AspNetConstants.CSharpExtension.Length]
            : ControllerName;
        if (!SyntaxFacts.IsValidIdentifier(controllerName) ||
            SyntaxFacts.GetKeywordKind(controllerName) != SyntaxKind.None ||
            IsReservedFileName(controllerName))
        {
            _logger.LogError($"Invalid {AspNetConstants.CliOptions.ControllerNameOption} option. " +
                "Specify a non-reserved, unqualified C# identifier, optionally followed by '.cs'.");
            return null;
        }

        if (addViews)
        {
            const string controllerSuffix = "Controller";
            string viewFolderName = controllerName.EndsWith(controllerSuffix, StringComparison.OrdinalIgnoreCase)
                ? controllerName[..^controllerSuffix.Length]
                : controllerName;
            if (string.IsNullOrEmpty(viewFolderName) || IsReservedFileName(viewFolderName))
            {
                _logger.LogError($"Invalid {AspNetConstants.CliOptions.ControllerNameOption} option. " +
                    "Specify a controller name with a nonempty, non-reserved view folder name when generating views.");
                return null;
            }
        }

        ControllerName = controllerName;

        if (string.IsNullOrEmpty(ControllerType))
        {
            _logger.LogError($"Missing/Invalid '{nameof(ControllerType)}' value.");
            return null;
        }
        else if (
            !string.IsNullOrEmpty(ControllerType) &&
            !ControllerType.Equals("API", StringComparison.OrdinalIgnoreCase) &&
            !ControllerType.Equals("MVC", StringComparison.OrdinalIgnoreCase))
        {
            //defaulting to API controller
            ControllerType = "API";
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

        return new EfControllerSettings
        {
            Model = Model,
            ControllerName = ControllerName,
            Project = Project,
            ControllerType = ControllerType,
            DataContext = DataContext,
            DatabaseProvider = DatabaseProvider,
            Prerelease = Prerelease
        };
    }

    private static bool IsReservedFileName(string name)
    {
        string upperName = name.ToUpperInvariant();
        return upperName is "CON" or "PRN" or "AUX" or "NUL" ||
            (upperName.Length == 4 &&
             (upperName.StartsWith("COM", StringComparison.Ordinal) || upperName.StartsWith("LPT", StringComparison.Ordinal)) &&
             (upperName[3] is >= '1' and <= '9' or '\u00b9' or '\u00b2' or '\u00b3'));
    }

    /// <summary>
    /// Initializes and returns the EfControllerModel for scaffolding.
    /// </summary>
    /// <param name="settings">EF Controller settings.</param>
    /// <param name="context">Scaffolder context.</param>
    /// <returns>Initialized EfControllerModel.</returns>
    private async Task<EfControllerModel?> GetEfControllerModelAsync(ScaffolderContext context, EfControllerSettings settings)
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
            _logger.LogError($"Invalid  {AspNetConstants.CliOptions.ModelCliOption}  '{settings.Model}'");
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

        EfControllerModel scaffoldingModel = new()
        {
            ControllerName = settings.ControllerName,
            ControllerType = settings.ControllerType,
            ProjectInfo = projectInfo,
            ModelInfo = modelInfo,
            DbContextInfo = dbContextInfo,
            ControllerOutputPath = Path.Combine(projectDirectory, AspNetConstants.DotnetCommands.ControllerCommandOutput)
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
