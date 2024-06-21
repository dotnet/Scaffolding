// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.DotNet.Scaffolding.Helpers.General;
using Microsoft.DotNet.Scaffolding.Helpers.Roslyn;
using Microsoft.DotNet.Scaffolding.Helpers.Services;
using Microsoft.DotNet.Scaffolding.Helpers.Services.Environment;
using Microsoft.DotNet.Scaffolding.Helpers.T4Templating;
using Microsoft.DotNet.Tools.Scaffold.AspNet.Helpers;
using Spectre.Console.Cli;

namespace Microsoft.DotNet.Tools.Scaffold.AspNet.Commands.MinimalApi;

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
        if (minimalApiModel.EfScenario)
        {
            _logger.LogMessage("Installing packages...");
            InstallPackages(settings);
            AddDbContext(minimalApiModel);
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

    private void AddDbContext(MinimalApiModel minimalApiModel)
    {
        //need to create a DbContext
        if (minimalApiModel.CreateDbContext && !string.IsNullOrEmpty(minimalApiModel.DatabaseProvider))
        {
            AspNetDbContextHelper.DatabaseTypeDefaults.TryGetValue(minimalApiModel.DatabaseProvider, out var dbContextProperties);
            if (dbContextProperties != null &&
                !string.IsNullOrEmpty(minimalApiModel.DbContextClassName) &&
                !string.IsNullOrEmpty(minimalApiModel.DbContextClassPath))
            {
                dbContextProperties.DbContextName = minimalApiModel.DbContextClassName;
                dbContextProperties.DbSetStatement = minimalApiModel.NewDbSetStatement;
                _logger.LogMessage($"Adding new DbContext '{dbContextProperties.DbContextName}'...");
                DbContextHelper.CreateDbContext(dbContextProperties, minimalApiModel.DbContextClassPath, _fileSystem);
            }
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
        var projectPath = minimalApiModel.AppSettings?.Workspace()?.InputPath;
        var allT4Templates = new TemplateFoldersUtilities().GetAllT4Templates(["MinimalApi"]);
        ITextTransformation? textTransformation = null;
        string? t4TemplatePath = null;
        if (minimalApiModel.EfScenario)
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

        if (minimalApiModel.AppSettings is not null && minimalApiModel.CodeService is not null && minimalApiModel.CodeChangeOptions is not null)
        {
            config = EditConfigForMinimalApi(config, minimalApiModel);
            var projectModifier = new ProjectModifier(
                _environmentService,
                minimalApiModel.AppSettings,
                minimalApiModel.CodeService,
                _logger,
                minimalApiModel.CodeChangeOptions,
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
            if (minimalApiModel.CodeChangeOptions is not null &&
                !minimalApiModel.CodeChangeOptions.UsingTopLevelsStatements &&
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

        if (minimalApiModel.EfScenario)
        {
            var efChangesFile = configToEdit.Files?.FirstOrDefault(x =>
                    !string.IsNullOrEmpty(x.FileName) &&
                    x.FileName.Equals("Program.cs", StringComparison.OrdinalIgnoreCase) &&
                    x.Options is not null &&
                    x.Options.Contains(CodeChangeOptionStrings.EfScenario));

            var efChangesGlobalMethod = efChangesFile?.Methods?.FirstOrDefault(x => x.Key.Equals("Global", StringComparison.OrdinalIgnoreCase)).Value;
            var addDbContextChange = efChangesGlobalMethod?.CodeChanges?.FirstOrDefault(x => x.Block.Contains("builder.Services.AddDbContext", StringComparison.OrdinalIgnoreCase));
            if (minimalApiModel.CreateDbContext &&
                addDbContextChange is not null &&
                !string.IsNullOrEmpty(minimalApiModel.DatabaseProvider) &&
                PackageConstants.EfConstants.UseDatabaseMethods.TryGetValue(minimalApiModel.DatabaseProvider, out var useDbMethod))
            {
                addDbContextChange.Block = string.Format(addDbContextChange.Block, minimalApiModel.DbContextClassName, useDbMethod);
            }

            if (string.IsNullOrEmpty(minimalApiModel.EntitySetVariableName) &&
                !minimalApiModel.CreateDbContext &&
                !string.IsNullOrEmpty(minimalApiModel.DbContextClassName) &&
                efChangesGlobalMethod != null)
            {
                var addDbStatementCodeChange = new CodeFile()
                {
                    FileName = StringUtil.EnsureCsExtension(minimalApiModel.DbContextClassName),
                    Options = [CodeChangeOptionStrings.EfScenario],
                    ClassProperties = [new CodeBlock
                    {
                        Block = minimalApiModel.NewDbSetStatement
                    }]
                };

                configToEdit.Files = configToEdit.Files?.Append(addDbStatementCodeChange).ToArray();
            }
        }

        return configToEdit;
    }

    private async Task<MinimalApiModel?> GetMinimalApiModelAsync(MinimalApiSettings settings)
    {
        MinimalApiModel scaffoldingModel = new();
        //set MinimalApiModel.CodeService
        var workspaceSettings = new WorkspaceSettings
        {
            InputPath = settings.Project
        };

        var projectAppSettings = new AppSettings();
        projectAppSettings.AddSettings("workspace", workspaceSettings);
        var codeService = new CodeService(projectAppSettings, _logger);
        scaffoldingModel.CodeService = codeService;
        scaffoldingModel.AppSettings = projectAppSettings;

        //find and set --model class properties
        var allClasses = await codeService.GetAllClassSymbolsAsync();
        var modelClassSymbol = allClasses.FirstOrDefault(x => x.Name.Equals(settings.Model, StringComparison.OrdinalIgnoreCase));
        if (string.IsNullOrEmpty(settings.Model) || modelClassSymbol is null)
        {
            _logger.LogMessage($"Invalid --model '{settings.Model}' provided", LogMessageType.Error);
            return null;
        }
        else
        {
            scaffoldingModel.ModelTypeName = settings.Model;
            scaffoldingModel.ModelNamespace = modelClassSymbol.ContainingNamespace.ToDisplayString();
            scaffoldingModel.EndpointsMethodName = $"Map{settings.Model}Endpoints";
            var efModelProperties = EfDbContextHelpers.GetModelProperties(modelClassSymbol);
            if (efModelProperties != null)
            {
                scaffoldingModel.PrimaryKeyName = efModelProperties.PrimaryKeyName;
                scaffoldingModel.PrimaryKeyShortTypeName = efModelProperties.PrimaryKeyShortTypeName;
                scaffoldingModel.PrimaryKeyTypeName = efModelProperties.PrimaryKeyTypeName;
                scaffoldingModel.ModelProperties = efModelProperties.AllModelProperties.Select(x => x.Name).ToList();
            }
        }

        //find endpoints class name and path
        var allDocs = await codeService.GetAllDocumentsAsync();
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
                scaffoldingModel.EndpointsPath = GetNewFilePath(scaffoldingModel, scaffoldingModel.EndpointsFileName);
            }
        }
        else
        {
            scaffoldingModel.EndpointsFileName = $"{settings.Model}Endpoints.cs";
            scaffoldingModel.EndpointsClassName = $"{settings.Model}Endpoints";
            scaffoldingModel.EndpointsPath = GetNewFilePath(scaffoldingModel, scaffoldingModel.EndpointsFileName);
        }

        //find DbContext info or create properties for a new one.
        var dbContextClassName = settings.DataContext;
        if (!string.IsNullOrEmpty(dbContextClassName))
        {
            scaffoldingModel.EfScenario = true;
            scaffoldingModel.DatabaseProvider = settings.DatabaseProvider;
            var existingDbContextClass = allClasses.FirstOrDefault(x => x.Name.Equals(dbContextClassName, StringComparison.OrdinalIgnoreCase));
            if (existingDbContextClass is not null)
            {
                scaffoldingModel.DbContextClassName = existingDbContextClass.Name;
                scaffoldingModel.DbContextClassPath = existingDbContextClass.Locations.FirstOrDefault()?.SourceTree?.FilePath;
                scaffoldingModel.DbContextNamespace = existingDbContextClass.ContainingNamespace.ToDisplayString();
                scaffoldingModel.EntitySetVariableName = EfDbContextHelpers.GetEntitySetVariableName(existingDbContextClass, scaffoldingModel.ModelTypeName);
            }
            //properties for creating a new DbContext
            else
            {
                scaffoldingModel.CreateDbContext = true;
                scaffoldingModel.DbContextClassName = dbContextClassName;
                scaffoldingModel.DbContextClassPath = GetNewFilePath(scaffoldingModel, scaffoldingModel.DbContextClassName);
                scaffoldingModel.DatabaseProvider = settings.DatabaseProvider;
                scaffoldingModel.EntitySetVariableName = scaffoldingModel.ModelTypeName;
            }
        }

        scaffoldingModel.OpenAPI = settings.OpenApi;
        scaffoldingModel.CodeChangeOptions = new CodeChangeOptions
        {
            IsMinimalApp = await ProjectModifierHelper.IsMinimalApp(codeService),
            UsingTopLevelsStatements = await ProjectModifierHelper.IsUsingTopLevelStatements(codeService),
            EfScenario = scaffoldingModel.EfScenario
        };

        return scaffoldingModel;
    }

    /// <summary>
    /// Given a class name (only meant for C# classes), get a file path at the base of the project (where the .csproj is on disk)
    /// </summary>
    /// <returns>string file path</returns>
    private string GetNewFilePath(MinimalApiModel templateModel, string className)
    {
        var newFilePath = string.Empty;
        var fileName = StringUtil.EnsureCsExtension(className);
        var baseProjectPath = Path.GetDirectoryName(templateModel.AppSettings?.Workspace().InputPath);
        if (!string.IsNullOrEmpty(baseProjectPath))
        {
            newFilePath = Path.Combine(baseProjectPath, $"{fileName}");
            newFilePath = StringUtil.GetUniqueFilePath(newFilePath);
        }

        return newFilePath;
    }

    private void InstallPackages(MinimalApiSettings commandSettings)
    {
        //add Microsoft.EntityFrameworkCore.Tools package regardless of the DatabaseProvider
        DotnetCommands.AddPackage(
            packageName: PackageConstants.EfConstants.EfToolsPackageName,
            logger: _logger,
            projectFile: commandSettings.Project,
            includePrerelease: commandSettings.Prerelease);

        if (!string.IsNullOrEmpty(commandSettings.DatabaseProvider) &&
            PackageConstants.EfConstants.EfPackagesDict.TryGetValue(commandSettings.DatabaseProvider, out string? projectPackageName))
        {
            DotnetCommands.AddPackage(
                packageName: projectPackageName,
                logger: _logger,
                projectFile: commandSettings.Project,
                includePrerelease: commandSettings.Prerelease);
        }
    }
}
