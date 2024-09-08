// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using Microsoft.DotNet.Scaffolding.Core.Scaffolders;
using Microsoft.DotNet.Scaffolding.Core.Steps;
using Microsoft.DotNet.Scaffolding.Internal;
using Microsoft.DotNet.Scaffolding.Internal.Services;
using Microsoft.DotNet.Scaffolding.TextTemplating.DbContext;
using Microsoft.DotNet.Tools.Scaffold.AspNet.Common;
using Microsoft.DotNet.Tools.Scaffold.AspNet.Helpers;
using Microsoft.DotNet.Tools.Scaffold.AspNet.Models;
using Microsoft.DotNet.Tools.Scaffold.AspNet.ScaffoldSteps.Settings;
using Microsoft.Extensions.Logging;
using Constants = Microsoft.DotNet.Scaffolding.Internal.Constants;

namespace Microsoft.DotNet.Tools.Scaffold.AspNet.ScaffoldSteps;

internal class ValidateMinimalApiStep : ScaffoldStep
{
    private readonly IFileSystem _fileSystem;
    private readonly ILogger _logger;

    public string? Project { get; set; }
    public bool Prerelease { get; set; }
    public string? Endpoints { get; set; }
    public bool OpenApi { get; set; } = true;
    public string? DatabaseProvider { get; set; }
    public string? DataContext { get; set; }
    public string? Model { get; set; }

    public ValidateMinimalApiStep(
        IFileSystem fileSystem,
        ILogger<ValidateMinimalApiStep> logger)
    {
        _fileSystem = fileSystem;
        _logger = logger;
    }

    public override async Task<bool> ExecuteAsync(ScaffolderContext context, CancellationToken cancellationToken = default)
    {
        var minimalApiSettings = ValidateMinimalApiSettings(context);
        var codeModifierProperties = new Dictionary<string, string>();
        if (minimalApiSettings is null)
        {
            return false;
        }
        else
        {
            context.Properties.Add(nameof(MinimalApiSettings), minimalApiSettings);
        }

        //initialize MinimalApiModel
        _logger.LogInformation("Initializing scaffolding model...");
        var minimalApiModel = await GetMinimalApiModelAsync(minimalApiSettings);
        if (minimalApiModel is null)
        {
            _logger.LogError("An error occurred.");
            return false;
        }
        else
        {
            context.Properties.Add(nameof(MinimalApiModel), minimalApiModel);
        }

        if (!string.IsNullOrEmpty(minimalApiModel.EndpointsMethodName))
        {
            codeModifierProperties.Add(Constants.CodeModifierPropertyConstants.EndpointsMethodName, minimalApiModel.EndpointsMethodName);
        }
        
        //Install packages and add a DbContext (if needed)
        if (minimalApiModel.DbContextInfo.EfScenario)
        {
            var dbContextProperties = AspNetDbContextHelper.GetDbContextProperties(minimalApiSettings.Project, minimalApiModel.DbContextInfo);
            if (dbContextProperties is not null)
            {
                context.Properties.Add(nameof(DbContextProperties), dbContextProperties);
            }

            var projectBasePath = Path.GetDirectoryName(minimalApiSettings.Project);
            if (!string.IsNullOrEmpty(projectBasePath))
            {
                context.Properties.Add(Constants.StepConstants.BaseProjectPath, projectBasePath);
            }

            var dbCodeModifierProperties = AspNetDbContextHelper.GetDbContextCodeModifierProperties(minimalApiModel.DbContextInfo);
            codeModifierProperties = codeModifierProperties
                .Concat(dbCodeModifierProperties)
                .ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
        }

        context.Properties.Add(Constants.StepConstants.CodeModifierProperties, codeModifierProperties);
        return true;
    }

    private MinimalApiSettings? ValidateMinimalApiSettings(ScaffolderContext context)
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

        if (!string.IsNullOrEmpty(DataContext) &&
            (string.IsNullOrEmpty(DatabaseProvider) || !PackageConstants.EfConstants.EfPackagesDict.ContainsKey(DatabaseProvider)))
        {
            DatabaseProvider = PackageConstants.EfConstants.SqlServer;
        }

        var commandSettings = new MinimalApiSettings
        {
            Project = Project,
            Model = Model,
            Prerelease = Prerelease,
            Endpoints = Endpoints,
            OpenApi = OpenApi,
            DataContext = DataContext,
            DatabaseProvider = DatabaseProvider,
        };

        return commandSettings;
    }

    private async Task<MinimalApiModel?> GetMinimalApiModelAsync(MinimalApiSettings settings)
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
            _logger.LogError($"Invalid --model '{settings.Model}' provided");
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
        }

        MinimalApiModel scaffoldingModel = new()
        {
            ProjectInfo = projectInfo,
            ModelInfo = modelInfo,
            DbContextInfo = dbContextInfo
        };

        //find endpoints class name and path
        var allDocs = await projectInfo.CodeService.GetAllDocumentsAsync();
        scaffoldingModel.EndpointsMethodName = $"Map{modelClassSymbol.Name}Endpoints";
        if (!string.IsNullOrEmpty(settings.Endpoints))
        {
            scaffoldingModel.EndpointsFileName = StringUtil.EnsureCsExtension(settings.Endpoints);
            var existingEndpointsDoc = allDocs.FirstOrDefault(x => x.Name.Equals(scaffoldingModel.EndpointsFileName, StringComparison.OrdinalIgnoreCase) || x.Name.EndsWith(scaffoldingModel.EndpointsFileName, StringComparison.OrdinalIgnoreCase));
            if (existingEndpointsDoc is not null)
            {
                scaffoldingModel.EndpointsPath = existingEndpointsDoc.FilePath ?? existingEndpointsDoc.Name;
            }
            else
            {
                scaffoldingModel.EndpointsClassName = Path.GetFileNameWithoutExtension(scaffoldingModel.EndpointsFileName);
                scaffoldingModel.EndpointsPath = CommandHelpers.GetNewFilePath(settings.Project, scaffoldingModel.EndpointsFileName);
            }
        }
        else
        {
            scaffoldingModel.EndpointsFileName = $"{settings.Model}Endpoints.cs";
            scaffoldingModel.EndpointsClassName = $"{settings.Model}Endpoints";
            scaffoldingModel.EndpointsPath = CommandHelpers.GetNewFilePath(settings.Project, scaffoldingModel.EndpointsFileName);
        }

        scaffoldingModel.OpenAPI = settings.OpenApi;
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
