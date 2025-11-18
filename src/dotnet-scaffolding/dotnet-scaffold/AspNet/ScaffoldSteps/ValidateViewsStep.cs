// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using Microsoft.DotNet.Scaffolding.Core.Logging;
using Microsoft.DotNet.Scaffolding.Core.Scaffolders;
using Microsoft.DotNet.Scaffolding.Core.Steps;
using Microsoft.DotNet.Scaffolding.Internal.Services;
using Microsoft.DotNet.Scaffolding.Internal.Telemetry;
using Microsoft.DotNet.Tools.Scaffold.AspNet.Common;
using Microsoft.DotNet.Tools.Scaffold.AspNet.Helpers;
using Microsoft.DotNet.Tools.Scaffold.AspNet.Models;
using Microsoft.DotNet.Tools.Scaffold.AspNet.ScaffoldSteps.Settings;
using Microsoft.DotNet.Tools.Scaffold.AspNet.Telemetry;
using AspNetConstants = Microsoft.DotNet.Tools.Scaffold.AspNet.Common.Constants;

namespace Microsoft.DotNet.Tools.Scaffold.AspNet.ScaffoldSteps;

/// <summary>
/// Scaffold step to validate View settings and initialize the ViewModel for scaffolding.
/// </summary>
internal class ValidateViewsStep : ScaffoldStep
{
    /// <summary>
    /// Path to the project file.
    /// </summary>
    public string? Project { get; set; }
    /// <summary>
    /// Name of the model class for the view.
    /// </summary>
    public string? Model { get; set; }
    /// <summary>
    /// Page type for view scaffolding.
    /// </summary>
    public string? Page { get; set; }

    private readonly IFileSystem _fileSystem;
    private readonly IScaffolderLogger _logger;
    private readonly ITelemetryService _telemetryService;

    /// <summary>
    /// Constructor for ValidateViewsStep.
    /// </summary>
    /// <param name="fileSystem">File system interface.</param>
    /// <param name="logger">Logger interface.</param>
    /// <param name="telemetryService">Telemetry service interface.</param>
    public ValidateViewsStep(
        IFileSystem fileSystem,
        IScaffolderLogger logger,
        ITelemetryService telemetryService)
    {
        _fileSystem = fileSystem;
        _logger = logger;
        _telemetryService = telemetryService;
    }

    /// <summary>
    /// Executes the step to validate View settings and initialize the ViewModel.
    /// </summary>
    /// <param name="context">Scaffolder context.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Task that represents the asynchronous operation, with a boolean result indicating success or failure.</returns>
    public override async Task<bool> ExecuteAsync(ScaffolderContext context, CancellationToken cancellationToken = default)
    {
        var viewSettings = ValidateViewsSettings();
        var codeModifierProperties = new Dictionary<string, string>();
        if (viewSettings is null)
        {
            _telemetryService.TrackEvent(new ValidateScaffolderTelemetryEvent(nameof(ValidateViewsStep), context.Scaffolder.DisplayName, result: false));
            return false;
        }
        else
        {
            context.Properties.Add(nameof(CrudSettings), viewSettings);
        }

        var viewModel = await GetViewModelAsync(viewSettings);
        if (viewModel is null)
        {
            _logger.LogError("An error occurred: 'ViewModel' instance could not be obtained");
            _telemetryService.TrackEvent(new ValidateScaffolderTelemetryEvent(nameof(ValidateViewsStep), context.Scaffolder.DisplayName, result: false));
            return false;
        }
        else
        {
            context.Properties.Add(nameof(ViewModel), viewModel);
        }

        _telemetryService.TrackEvent(new ValidateScaffolderTelemetryEvent(nameof(ValidateViewsStep), context.Scaffolder.DisplayName, result: true));
        return true;
    }

    /// <summary>
    /// Validates the View settings provided by the user.
    /// </summary>
    /// <returns>CrudSettings object if valid, null otherwise.</returns>
    private CrudSettings? ValidateViewsSettings()
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

        return new CrudSettings
        {
            Model = Model,
            Project = Project,
            Page = Page
        };
    }

    /// <summary>
    /// Initializes and returns the ViewModel for scaffolding.
    /// </summary>
    /// <param name="settings">CrudSettings object containing the settings for scaffolding.</param>
    /// <returns>Task that represents the asynchronous operation, with a ViewModel result if successful, null otherwise.</returns>
    private async Task<ViewModel?> GetViewModelAsync(CrudSettings settings)
    {
        var projectInfo = ClassAnalyzers.GetProjectInfo(settings.Project, _logger);
        if (projectInfo is null || projectInfo.CodeService is null)
        {
            return null;
        }

        //find and set --model class properties
        ModelInfo? modelInfo = null;
        var allClasses = await projectInfo.CodeService.GetAllClassSymbolsAsync();
        var modelClassSymbol = allClasses.FirstOrDefault(x => x.Name.Equals(settings.Model, StringComparison.OrdinalIgnoreCase));
        if (string.IsNullOrEmpty(settings.Model) || modelClassSymbol is null)
        {
            _logger.LogError($"Invalid {AspNetConstants.CliOptions.ModelCliOption} '{settings.Model}'");
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

        ViewModel scaffoldingModel = new()
        {
            PageType = settings.Page,
            ProjectInfo = projectInfo,
            ModelInfo = modelInfo,
            DbContextInfo = new DbContextInfo()
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
