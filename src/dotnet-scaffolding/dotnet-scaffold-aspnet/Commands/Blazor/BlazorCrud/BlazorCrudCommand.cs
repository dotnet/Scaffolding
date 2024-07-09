// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.DotNet.Scaffolding.Helpers.Roslyn;
using Microsoft.DotNet.Scaffolding.Helpers.Services;
using Microsoft.DotNet.Scaffolding.Helpers.Services.Environment;
using Microsoft.DotNet.Scaffolding.TextTemplating;
using Microsoft.DotNet.Tools.Scaffold.AspNet.Commands.Common;
using Microsoft.DotNet.Tools.Scaffold.AspNet.Helpers;
using Spectre.Console.Cli;
using Microsoft.DotNet.Scaffolding.Helpers.Steps;

namespace Microsoft.DotNet.Tools.Scaffold.AspNet.Commands.Blazor.BlazorCrud;

internal class BlazorCrudCommand : AsyncCommand<BlazorCrudSettings>
{
    private readonly IAppSettings _appSettings;
    private readonly IFileSystem _fileSystem;
    private readonly ILogger _logger;
    private readonly IEnvironmentService _environmentService;
    private readonly IHostService _hostService;
    private readonly ICodeService _codeService;
    private List<string> _excludeList;

    public BlazorCrudCommand(
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

    public override async Task<int> ExecuteAsync([NotNull] CommandContext context, [NotNull] BlazorCrudSettings settings)
    {
        new MsBuildInitializer(_logger).Initialize();
        if (!ValidateBlazorCrudSettings(settings))
        {
            return -1;
        }

        //initialize BlazorCrudModel
        _logger.LogMessage("Initializing scaffolding model...");
        var blazorCrudModel = await GetBlazorCrudModelAsync(settings);
        if (blazorCrudModel is null)
        {
            _logger.LogMessage("An error occurred.");
            return -1;
        }

        //Install packages and add a DbContext (if needed)
        if (blazorCrudModel.DbContextInfo.EfScenario)
        {
            _logger.LogMessage("Installing packages...");
            await InstallPackagesAsync(settings);
            blazorCrudModel.DbContextInfo.AddDbContext(blazorCrudModel.ProjectInfo, _logger, _fileSystem);
        }

        _logger.LogMessage("Adding razor components...");
        var executeTemplateResult = ExecuteTemplates(blazorCrudModel);
        //if we were not able to execute the templates successfully,
        //exit and don't update the project
        if (!executeTemplateResult)
        {
            _logger.LogMessage("An error occurred.");
            return -1;
        }

        //Update the project's Program.cs file
        _logger.LogMessage("Updating project...");
        var projectUpdateResult = await UpdateProjectAsync(blazorCrudModel);
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

    private bool ValidateBlazorCrudSettings(BlazorCrudSettings commandSettings)
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

        if (string.IsNullOrEmpty(commandSettings.Page))
        {
            _logger.LogMessage("Missing/Invalid --page option.", LogMessageType.Error);
            return false;
        }
        else if (!string.IsNullOrEmpty(commandSettings.Page) && !BlazorCrudHelper.CRUDPages.Contains(commandSettings.Page, StringComparer.OrdinalIgnoreCase))
        {
            //if an invalid page name, switch to just "CRUD" and scaffold all pages
            commandSettings.Page = BlazorCrudHelper.CrudPageType;
        }

        if (string.IsNullOrEmpty(commandSettings.DataContext))
        {
            _logger.LogMessage("Missing/Invalid --dataContext option.", LogMessageType.Error);
            return false;
        }
        else if (string.IsNullOrEmpty(commandSettings.DatabaseProvider) || !PackageConstants.EfConstants.EfPackagesDict.ContainsKey(commandSettings.DatabaseProvider))
        {
            commandSettings.DatabaseProvider = PackageConstants.EfConstants.SqlServer;
        }

        return true;
    }

    private bool ExecuteTemplates(BlazorCrudModel blazorCrudModel)
    {
        var projectPath = blazorCrudModel.ProjectInfo.AppSettings?.Workspace()?.InputPath;
        var allT4TemplatePaths = new TemplateFoldersUtilities().GetAllT4Templates(["BlazorCrud"]);
        var neededT4FileNames = BlazorCrudHelper.GetT4Templates(blazorCrudModel.PageType);
        // Create a HashSet for quick lookup of needed file names
        var neededFileNamesSet = new HashSet<string>(neededT4FileNames, StringComparer.OrdinalIgnoreCase);

        // Filter the paths based on the presence of the file names in the hash set
        var matchingPaths = allT4TemplatePaths
            .Where(path => neededFileNamesSet.Contains(Path.GetFileName(path)))
            .ToList();

        var dictParams = new Dictionary<string, object>()
        {
            { "Model" , blazorCrudModel }
        };

        foreach (var templatePath in matchingPaths)
        {
            ITextTransformation? textTransformation = null;
            textTransformation = BlazorCrudHelper.GetBlazorCrudTransformation(templatePath);
            if (textTransformation is null)
            {
                throw new Exception($"Unable to process T4 template '{templatePath}' correctly");
            }

            var templateInvoker = new TemplateInvoker();
            var templatedString = templateInvoker.InvokeTemplate(textTransformation, dictParams);
            string outputPath = BlazorCrudHelper.ValidateAndGetOutputPath(
                blazorCrudModel.ModelInfo.ModelTypeName,
                Path.GetFileNameWithoutExtension(templatePath),
                blazorCrudModel.ProjectInfo.AppSettings?.Workspace().InputPath);

            var outputDirectory = Path.GetDirectoryName(outputPath);
            if (!string.IsNullOrEmpty(templatedString) &&
                !string.IsNullOrEmpty(outputPath) &&
                !string.IsNullOrEmpty(outputDirectory))
            {
                if (!_fileSystem.DirectoryExists(outputDirectory))
                {
                    _fileSystem.CreateDirectory(outputDirectory);
                }

                _fileSystem.WriteAllText(outputPath, templatedString);
            }
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
            var projectModifier = new ProjectModifier(
                _environmentService,
                blazorCrudModel.ProjectInfo.AppSettings,
                blazorCrudModel.ProjectInfo.CodeService,
                _logger,
                blazorCrudModel.ProjectInfo.CodeChangeOptions,
                config);

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
            _logger.LogMessage($"Invalid --model '{settings.Model}'", LogMessageType.Error);
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

        if (!string.IsNullOrEmpty(dbContextClassName))
        {
            var dbContextClassSymbol = allClasses.FirstOrDefault(x => x.Name.Equals(dbContextClassName, StringComparison.OrdinalIgnoreCase));
            dbContextInfo = ClassAnalyzers.GetDbContextInfo(dbContextClassSymbol, projectInfo.AppSettings, dbContextClassName, settings.DatabaseProvider, settings.Model);
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
        var packageList = new List<string?>()
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
