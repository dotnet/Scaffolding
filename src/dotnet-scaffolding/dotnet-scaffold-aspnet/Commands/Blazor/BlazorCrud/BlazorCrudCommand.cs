// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.DotNet.Scaffolding.Core.Scaffolders;
using Microsoft.DotNet.Scaffolding.Helpers.General;
using Microsoft.DotNet.Scaffolding.Helpers.Roslyn;
using Microsoft.DotNet.Scaffolding.Helpers.Services;
using Microsoft.DotNet.Scaffolding.Helpers.Steps;
using Microsoft.DotNet.Tools.Scaffold.AspNet.Commands.Common;
using Microsoft.DotNet.Tools.Scaffold.AspNet.Commands.Settings;
using Microsoft.DotNet.Tools.Scaffold.AspNet.Helpers;
using Microsoft.Extensions.Logging;

namespace Microsoft.DotNet.Tools.Scaffold.AspNet.Commands.Blazor.BlazorCrud;

internal class BlazorCrudCommand : ICommandWithSettings<BlazorCrudSettings>
{
    private readonly IFileSystem _fileSystem;
    private readonly ILogger _logger;
    public BlazorCrudCommand(
        IFileSystem fileSystem,
        ILogger logger)
    {
        _fileSystem = fileSystem;
        _logger = logger;
    }

    public async Task<int> ExecuteAsync(BlazorCrudSettings settings, ScaffolderContext context)
    {
        if (!ValidateBlazorCrudSettings(settings))
        {
            return -1;
        }

        //initialize BlazorCrudModel
        _logger.LogInformation("Initializing scaffolding model...");
        var blazorCrudModel = await GetBlazorCrudModelAsync(settings);
        if (blazorCrudModel is null)
        {
            _logger.LogError("An error occurred.");
            return -1;
        }
        else
        {
            context.Properties.Add(nameof(BlazorCrudModel), blazorCrudModel);
        }

        //Install packages and add a DbContext (if needed)
        if (blazorCrudModel.DbContextInfo.EfScenario)
        {
            _logger.LogInformation("Installing packages...");
            await InstallPackagesAsync(settings);
            var dbContextProperties = AspNetDbContextHelper.GetDbContextProperties(blazorCrudModel.DbContextInfo, blazorCrudModel.ProjectInfo);
            if (dbContextProperties is not null)
            {
                context.Properties.Add(nameof(DbContextProperties), dbContextProperties);
            }

            var projectPath = blazorCrudModel.ProjectInfo?.AppSettings?.Workspace()?.InputPath;
            var projectBasePath = Path.GetDirectoryName(projectPath);
            if (!string.IsNullOrEmpty(projectBasePath))
            {
                context.Properties.Add("BaseProjectPath", projectBasePath);
            }
        }

        //Update the project's Program.cs file
        _logger.LogInformation("Updating project...");
        var projectUpdateResult = await UpdateProjectAsync(blazorCrudModel);
        if (projectUpdateResult)
        {
            _logger.LogInformation("Finished");
            return 0;
        }
        else
        {
            _logger.LogError("An error occurred.");
            return -1;
        }
    }

    private bool ValidateBlazorCrudSettings(BlazorCrudSettings commandSettings)
    {
        if (string.IsNullOrEmpty(commandSettings.Project) || !_fileSystem.FileExists(commandSettings.Project))
        {
            _logger.LogError("Missing/Invalid --project option.");
            return false;
        }

        if (string.IsNullOrEmpty(commandSettings.Model))
        {
            _logger.LogError("Missing/Invalid --model option.");
            return false;
        }

        if (string.IsNullOrEmpty(commandSettings.Page))
        {
            _logger.LogError("Missing/Invalid --page option.");
            return false;
        }
        else if (!string.IsNullOrEmpty(commandSettings.Page) && !BlazorCrudHelper.CRUDPages.Contains(commandSettings.Page, StringComparer.OrdinalIgnoreCase))
        {
            //if an invalid page name, switch to just "CRUD" and scaffold all pages
            commandSettings.Page = BlazorCrudHelper.CrudPageType;
        }

        if (string.IsNullOrEmpty(commandSettings.DataContext))
        {
            _logger.LogError("Missing/Invalid --dataContext option.");
            return false;
        }
        else if (string.IsNullOrEmpty(commandSettings.DatabaseProvider) || !PackageConstants.EfConstants.EfPackagesDict.ContainsKey(commandSettings.DatabaseProvider))
        {
            commandSettings.DatabaseProvider = PackageConstants.EfConstants.SqlServer;
        }

        return true;
    }

    private async Task<bool> UpdateProjectAsync(BlazorCrudModel blazorCrudModel)
    {
        CodeModifierConfig? config = ProjectModifierHelper.GetCodeModifierConfig("blazorWebCrudChanges.json", System.Reflection.Assembly.GetExecutingAssembly());
        if (blazorCrudModel.ProjectInfo.AppSettings is not null &&
            blazorCrudModel.ProjectInfo.CodeService is not null &&
            blazorCrudModel.ProjectInfo.CodeChangeOptions is not null &&
            config is not null)
        {
            config = await EditConfigForBlazorCrudAsync(config, blazorCrudModel);
            var codeChangeOptions = new CodeChangeOptions()
            {
                IsMinimalApp = await ProjectModifierHelper.IsMinimalApp(blazorCrudModel.ProjectInfo.CodeService),
                UsingTopLevelsStatements = await ProjectModifierHelper.IsUsingTopLevelStatements(blazorCrudModel.ProjectInfo.CodeService),
                EfScenario = blazorCrudModel.DbContextInfo.EfScenario
            };

            var projectModifier = new ProjectModifier(
                blazorCrudModel.ProjectInfo.AppSettings.Workspace().InputPath ?? string.Empty,
                blazorCrudModel.ProjectInfo.CodeService,
                _logger,
                config,
                codeChangeOptions);

            return await projectModifier.RunAsync();
        }

        return false;
    }

    private async Task<CodeModifierConfig> EditConfigForBlazorCrudAsync(CodeModifierConfig configToEdit, BlazorCrudModel blazorCrudModel)
    {
        var codeService = blazorCrudModel.ProjectInfo?.CodeService;
        if (codeService is not null)
        {
            var programCsDocument = await codeService.GetDocumentAsync("Program.cs");
            var appRazorDocument = await codeService.GetDocumentAsync("App.razor");
            var blazorAppProperties = await BlazorCrudHelper.GetBlazorPropertiesAsync(programCsDocument, appRazorDocument);
            configToEdit = BlazorCrudHelper.AddBlazorChangesToCodeFile(configToEdit, blazorAppProperties);
        }

        configToEdit = AspNetDbContextHelper.AddDbContextChanges(blazorCrudModel.DbContextInfo, configToEdit);
        return configToEdit;
    }

    private async Task<BlazorCrudModel?> GetBlazorCrudModelAsync(BlazorCrudSettings settings)
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
            dbContextInfo = ClassAnalyzers.GetDbContextInfo(dbContextClassSymbol, projectInfo.AppSettings, dbContextClassName, settings.DatabaseProvider, settings.Model);
            dbContextInfo.EfScenario = true;
        }

        BlazorCrudModel scaffoldingModel = new()
        {
            PageType = settings.Page,
            ProjectInfo = projectInfo,
            ModelInfo = modelInfo,
            DbContextInfo = dbContextInfo
        };

        if (scaffoldingModel.ProjectInfo is not null && scaffoldingModel.ProjectInfo.CodeService is not null)
        {
            scaffoldingModel.ProjectInfo.CodeChangeOptions = new CodeChangeOptions
            {
                IsMinimalApp = await ProjectModifierHelper.IsMinimalApp(scaffoldingModel.ProjectInfo.CodeService),
                UsingTopLevelsStatements = await ProjectModifierHelper.IsUsingTopLevelStatements(scaffoldingModel.ProjectInfo.CodeService),
                EfScenario = scaffoldingModel.DbContextInfo.EfScenario
            };
        }

        return scaffoldingModel;
    }

    private async Task InstallPackagesAsync(BlazorCrudSettings commandSettings)
    {
        //add these packages regardless of the DatabaseProvider
        var packageList = new List<string>()
        {
            PackageConstants.EfConstants.EfToolsPackageName,
            PackageConstants.EfConstants.QuickGridEfAdapterPackageName,
            PackageConstants.EfConstants.AspNetCoreDiagnosticsEfCorePackageName
        };

        if (!string.IsNullOrEmpty(commandSettings.DatabaseProvider) &&
           PackageConstants.EfConstants.EfPackagesDict.TryGetValue(commandSettings.DatabaseProvider, out string? projectPackageName))
        {
            packageList.Add(projectPackageName);
        }

        await new AddPackagesStep
        {
            PackageNames = packageList,
            ProjectPath = commandSettings.Project,
            Prerelease = commandSettings.Prerelease,
            Logger = _logger
        }.ExecuteAsync();
    }
}
