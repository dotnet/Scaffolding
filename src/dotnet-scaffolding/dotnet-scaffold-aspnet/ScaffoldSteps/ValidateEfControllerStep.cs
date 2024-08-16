// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using Microsoft.DotNet.Scaffolding.Core.Scaffolders;
using Microsoft.DotNet.Scaffolding.Core.Steps;
using Microsoft.DotNet.Scaffolding.Internal.Services;
using Microsoft.DotNet.Scaffolding.TextTemplating.DbContext;
using Microsoft.DotNet.Tools.Scaffold.AspNet.Common;
using Microsoft.DotNet.Tools.Scaffold.AspNet.Helpers;
using Microsoft.DotNet.Tools.Scaffold.AspNet.Models;
using Microsoft.DotNet.Tools.Scaffold.AspNet.ScaffoldSteps.Settings;
using Microsoft.Extensions.Logging;
using Constants = Microsoft.DotNet.Scaffolding.Internal.Constants;

namespace Microsoft.DotNet.Tools.Scaffold.AspNet.ScaffoldSteps;

internal class ValidateEfControllerStep : ScaffoldStep
{
    private readonly IFileSystem _fileSystem;
    private readonly ILogger _logger;
    public string? Project { get; set; }
    public bool Prerelease { get; set; }
    public string? DatabaseProvider { get; set; }
    public string? DataContext { get; set; }
    public string? Model { get; set; }
    public string? ControllerName { get; set; }
    public string? ControllerType { get; set; }

    public ValidateEfControllerStep(
        IFileSystem fileSystem,
        ILogger<ValidateEfControllerStep> logger)
    {
        _fileSystem = fileSystem;
        _logger = logger;
    }

    public override async Task<bool> ExecuteAsync(ScaffolderContext context, CancellationToken cancellationToken = default)
    {
        var efControllerSettings = ValidateEfControllerSettings();
        var codeModifierProperties = new Dictionary<string, string>();
        if (efControllerSettings is null)
        {
            return false;
        }
        else
        {
            context.Properties.Add(nameof(EfControllerSettings), efControllerSettings);
        }

        //initialize CrudControllerModel
        _logger.LogInformation("Initializing scaffolding model...");
        var efControllerModel = await GetEfControllerModelAsync(efControllerSettings);
        if (efControllerModel is null)
        {
            _logger.LogError("An error occurred.");
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
        return true;
    }

    private EfControllerSettings? ValidateEfControllerSettings()
    {
        if (string.IsNullOrEmpty(Project) || !_fileSystem.FileExists(Project))
        {
            _logger.LogError("Missing/Invalid --project option.");
            return null;
        }

        if (string.IsNullOrEmpty(Model))
        {
            _logger.LogError("Missing/Invalid --model option.");
            return null;
        }

        if (string.IsNullOrEmpty(ControllerName))
        {
            _logger.LogError("Missing/Invalid --controller option.");
            return null;
        }
        else
        {
            ControllerName = Path.GetFileNameWithoutExtension(ControllerName);
        }

        if (string.IsNullOrEmpty(ControllerType))
        {
            _logger.LogError($"Missing/Invalid '{nameof(ValidateEfControllerStep.ControllerType)}' value.");
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
            _logger.LogError("Missing/Invalid --dataContext option.");
            return null;
        }
        else if (string.IsNullOrEmpty(DatabaseProvider) || !PackageConstants.EfConstants.EfPackagesDict.ContainsKey(DatabaseProvider))
        {
            DatabaseProvider = PackageConstants.EfConstants.SqlServer;
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

    private async Task<EfControllerModel?> GetEfControllerModelAsync(EfControllerSettings settings)
    {
        var projectInfo = ClassAnalyzers.GetProjectInfo(settings.Project, _logger);
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
            _logger.LogError($"Invalid --model '{settings.Model}'");
            return null;
        }
        else
        {
            modelInfo = ClassAnalyzers.GetModelClassInfo(modelClassSymbol);
        }

        var validateModelInfoResult = ClassAnalyzers.ValidateModelForCrudScaffolders(modelInfo, _logger);
        if (!validateModelInfoResult)
        {
            _logger.LogError($"Invalid --model '{settings.Model}'");
            return null;
        }

        //find DbContext info or create properties for a new one.
        var dbContextClassName = settings.DataContext;
        DbContextInfo dbContextInfo = new();

        if (!string.IsNullOrEmpty(dbContextClassName) && !string.IsNullOrEmpty(settings.DatabaseProvider))
        {
            var dbContextClassSymbol = allClasses.FirstOrDefault(x => x.Name.Equals(dbContextClassName, StringComparison.OrdinalIgnoreCase));
            dbContextInfo = ClassAnalyzers.GetDbContextInfo(settings.Project, dbContextClassSymbol, dbContextClassName, settings.DatabaseProvider, settings.Model);
            dbContextInfo.EfScenario = true;
        }

        EfControllerModel scaffoldingModel = new()
        {
            ControllerName = settings.ControllerName,
            ControllerType = settings.ControllerType,
            ProjectInfo = projectInfo,
            ModelInfo = modelInfo,
            DbContextInfo = dbContextInfo,
            ControllerOutputPath = projectDirectory
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
