// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.DotNet.Scaffolding.Helpers.General;
using Microsoft.DotNet.Scaffolding.Helpers.Roslyn;
using Microsoft.DotNet.Scaffolding.Helpers.Services;
using Microsoft.DotNet.Scaffolding.Helpers.Services.Environment;
using Microsoft.DotNet.Scaffolding.TextTemplating;
using Microsoft.DotNet.Tools.Scaffold.AspNet.Commands.Common;
using Microsoft.DotNet.Tools.Scaffold.AspNet.Helpers;
using Spectre.Console.Cli;
using Microsoft.DotNet.Scaffolding.Helpers.Steps;

namespace Microsoft.DotNet.Tools.Scaffold.AspNet.Commands.API.MinimalApi;

internal class MinimalApiCommand : AsyncCommand<MinimalApiSettings>
{
    private readonly IAppSettings _appSettings;
    private readonly IFileSystem _fileSystem;
    private readonly ILogger _logger;
    private readonly IEnvironmentService _environmentService;
    private readonly IHostService _hostService;
    private readonly ICodeService _codeService;
    private List<string> _excludeList;

    public MinimalApiCommand(
        IAppSettings appSettings,
        IEnvironmentService environmentService,
        IFileSystem fileSystem,
        IHostService hostService,
        ICodeService codeService,
        ILogger logger)
    {
        _appSettings = appSettings;
        _environmentService = environmentService;
        _fileSystem = fileSystem;
        _hostService = hostService;
        _logger = logger;
        _codeService = codeService;
        _excludeList = [];
    }

    public override async Task<int> ExecuteAsync([NotNull] CommandContext context, [NotNull] MinimalApiSettings settings)
    {
        new MsBuildInitializer(_logger).Initialize();
        if (!ValidateMinimalApiSettings(settings))
        {
            return -1;
        }

        //initialize MinimalApiModel
        _logger.LogMessage("Initializing scaffolding model...");
        var minimalApiModel = await GetMinimalApiModelAsync(settings);
        if (minimalApiModel is null)
        {
            _logger.LogMessage("An error occurred.");
            return -1;
        }

        //Install packages and add a DbContext (if needed)
        if (minimalApiModel.DbContextInfo.EfScenario)
        {
            _logger.LogMessage("Installing packages...");
            await InstallPackagesAsync(settings);
            minimalApiModel.DbContextInfo.AddDbContext(minimalApiModel.ProjectInfo, _logger, _fileSystem);
        }

        _logger.LogMessage("Adding API controller...");
        var executeTemplateResult = ExecuteTemplates(minimalApiModel);
        //if we were not able to execute the templates successfully,
        //exit and don't update the project
        if (!executeTemplateResult)
        {
            _logger.LogMessage("An error occurred.");
            return -1;
        }

        //Update the project's Program.cs file
        _logger.LogMessage("Updating project...");
        var projectUpdateResult = await UpdateProjectAsync(minimalApiModel);
        if (projectUpdateResult)
        {
            _logger.LogMessage("Finished");
            return 0;
        }
        else
        {
            _logger.LogMessage("An error occurred.");
            return -1;
        }
    }



    private bool ValidateMinimalApiSettings(MinimalApiSettings commandSettings)
    {
        if (string.IsNullOrEmpty(commandSettings.Project) || !_fileSystem.FileExists(commandSettings.Project))
        {
            _logger.LogMessage("Missing/Invalid --project option.", LogMessageType.Error);
            return false;
        }

        if (string.IsNullOrEmpty(commandSettings.Model))
        {
            _logger.LogMessage("Missing/Invalid --model option.", LogMessageType.Error);
            return false;
        }

        if (!string.IsNullOrEmpty(commandSettings.DataContext) &&
            (string.IsNullOrEmpty(commandSettings.DatabaseProvider) || !PackageConstants.EfConstants.EfPackagesDict.ContainsKey(commandSettings.DatabaseProvider)))
        {
            commandSettings.DatabaseProvider = PackageConstants.EfConstants.SqlServer;
        }

        return true;
    }

    private bool ExecuteTemplates(MinimalApiModel minimalApiModel)
    {
        var projectPath = minimalApiModel.ProjectInfo.AppSettings?.Workspace()?.InputPath;
        var allT4Templates = new TemplateFoldersUtilities().GetAllT4Templates(["MinimalApi"]);
        ITextTransformation? textTransformation = null;
        string? t4TemplatePath = null;
        if (minimalApiModel.DbContextInfo.EfScenario)
        {
            t4TemplatePath = allT4Templates.FirstOrDefault(x => x.EndsWith("MinimalApiEf.tt", StringComparison.OrdinalIgnoreCase));
        }
        else
        {
            t4TemplatePath = allT4Templates.FirstOrDefault(x => x.EndsWith("MinimalApi.tt", StringComparison.OrdinalIgnoreCase));
        }

        textTransformation = GetMinimalApiTransformation(t4TemplatePath);
        if (textTransformation is null)
        {
            throw new Exception($"Unable to process T4 template '{t4TemplatePath}' correctly");
        }

        var templateInvoker = new TemplateInvoker();
        var dictParams = new Dictionary<string, object>()
        {
            { "Model" , minimalApiModel }
        };

        var templatedString = templateInvoker.InvokeTemplate(textTransformation, dictParams);
        if (!string.IsNullOrEmpty(templatedString) && !string.IsNullOrEmpty(minimalApiModel.EndpointsPath))
        {
            _fileSystem.WriteAllText(minimalApiModel.EndpointsPath, templatedString);
            return true;
        }

        return false;
    }

    private static ITextTransformation? GetMinimalApiTransformation(string? templatePath)
    {
        if (string.IsNullOrEmpty(templatePath))
        {
            return null;
        }

        var host = new TextTemplatingEngineHost { TemplateFile = templatePath };
        ITextTransformation? transformation = null;

        switch (Path.GetFileName(templatePath))
        {
            case "MinimalApi.tt":
                transformation = new Templates.MinimalApi.MinimalApi() { Host = host };
                break;
            case "MinimalApiEf.tt":
                transformation = new Templates.MinimalApi.MinimalApiEf() { Host = host };
                break;
        }

        if (transformation is not null)
        {
            transformation.Session = host.CreateSession();
        }

        return transformation;
    }

    private async Task<bool> UpdateProjectAsync(MinimalApiModel minimalApiModel)
    {
        CodeModifierConfig? config = ProjectModifierHelper.GetCodeModifierConfig("minimalApiChanges.json", System.Reflection.Assembly.GetExecutingAssembly());
        if (minimalApiModel.ProjectInfo.AppSettings is not null && minimalApiModel.ProjectInfo.CodeService is not null && minimalApiModel.ProjectInfo.CodeChangeOptions is not null)
        {
            config = EditConfigForMinimalApi(config, minimalApiModel);
            var projectModifier = new ProjectModifier(
                _environmentService,
                minimalApiModel.ProjectInfo.AppSettings,
                minimalApiModel.ProjectInfo.CodeService,
                _logger,
                minimalApiModel.ProjectInfo.CodeChangeOptions,
                config);

            return await projectModifier.RunAsync();
        }

        return false;
    }

    private CodeModifierConfig? EditConfigForMinimalApi(CodeModifierConfig? configToEdit, MinimalApiModel minimalApiModel)
    {
        if (configToEdit is null)
        {
            return null;
        }

        var programCsFile = configToEdit.Files?.FirstOrDefault(x => !string.IsNullOrEmpty(x.FileName) && x.FileName.Equals("Program.cs", StringComparison.OrdinalIgnoreCase));
        var globalMethod = programCsFile?.Methods?.FirstOrDefault(x => x.Key.Equals("Global", StringComparison.OrdinalIgnoreCase)).Value;
        if (globalMethod is not null)
        {
            //only one change in here
            var addEndpointsChange = globalMethod?.CodeChanges?.FirstOrDefault();
            if (minimalApiModel.ProjectInfo.CodeChangeOptions is not null &&
                !minimalApiModel.ProjectInfo.CodeChangeOptions.UsingTopLevelsStatements &&
                addEndpointsChange is not null)
            {
                addEndpointsChange = DocumentBuilder.AddLeadingTriviaSpaces(addEndpointsChange, spaces: 12);
            }

            if (addEndpointsChange is not null &&
                !string.IsNullOrEmpty(addEndpointsChange.Block) &&
                !string.IsNullOrEmpty(minimalApiModel.EndpointsMethodName))
            {
                //formatting the endpoints method call onto "app.{0}()"
                addEndpointsChange.Block = string.Format(addEndpointsChange.Block, minimalApiModel.EndpointsMethodName);
            }
        }

        var dbContextInfo = minimalApiModel.DbContextInfo;
        configToEdit = AspNetDbContextHelper.AddDbContextChanges(dbContextInfo, configToEdit);
        return configToEdit;
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
            _logger.LogMessage($"Invalid --model '{settings.Model}' provided", LogMessageType.Error);
            return null;
        }
        else
        {
            modelInfo = ClassAnalyzers.GetModelClassInfo(modelClassSymbol);
        }

        var validateModelInfoResult = ClassAnalyzers.ValidateModelForCrudScaffolders(modelInfo, _logger);
        if (!validateModelInfoResult)
        {
            _logger.LogMessage($"Invalid --model '{settings.Model}'", LogMessageType.Error);
            return null;
        }

        //find DbContext info or create properties for a new one.
        var dbContextClassName = settings.DataContext;
        DbContextInfo dbContextInfo = new();

        if (!string.IsNullOrEmpty(dbContextClassName) && !string.IsNullOrEmpty(settings.DatabaseProvider))
        {
            var dbContextClassSymbol = allClasses.FirstOrDefault(x => x.Name.Equals(dbContextClassName, StringComparison.OrdinalIgnoreCase));
            dbContextInfo = ClassAnalyzers.GetDbContextInfo(dbContextClassSymbol, projectInfo.AppSettings, dbContextClassName, settings.DatabaseProvider, settings.Model);
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
                scaffoldingModel.EndpointsPath = CommandHelpers.GetNewFilePath(scaffoldingModel.ProjectInfo.AppSettings, scaffoldingModel.EndpointsFileName);
            }
        }
        else
        {
            scaffoldingModel.EndpointsFileName = $"{settings.Model}Endpoints.cs";
            scaffoldingModel.EndpointsClassName = $"{settings.Model}Endpoints";
            scaffoldingModel.EndpointsPath = CommandHelpers.GetNewFilePath(scaffoldingModel.ProjectInfo.AppSettings, scaffoldingModel.EndpointsFileName);
        }



        scaffoldingModel.OpenAPI = settings.OpenApi;
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

    private async Task InstallPackagesAsync(MinimalApiSettings commandSettings)
    {
        //add Microsoft.EntityFrameworkCore.Tools package regardless of the DatabaseProvider
        var packageList = new List<string?>()
        {
            PackageConstants.EfConstants.EfToolsPackageName
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
