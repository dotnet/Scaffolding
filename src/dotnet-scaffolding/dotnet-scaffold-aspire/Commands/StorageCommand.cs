// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.DotNet.Scaffolding.Helpers.Extensions.Roslyn;
using Microsoft.DotNet.Scaffolding.Helpers.Roslyn;
using Microsoft.DotNet.Scaffolding.Helpers.Services;
using Microsoft.DotNet.Scaffolding.Helpers.Services.Environment;
using Microsoft.DotNet.Scaffolding.Helpers.Steps.AddPackageStep;
using Microsoft.DotNet.Tools.Scaffold.Aspire.Helpers;
using Spectre.Console.Cli;

namespace Microsoft.DotNet.Tools.Scaffold.Aspire.Commands;

internal class StorageCommand : AsyncCommand<StorageCommand.StorageCommandSettings>
{
    private readonly ILogger _logger;
    private readonly IFileSystem _fileSystem;
    private readonly IEnvironmentService _environmentService;
    //Dictionary to hold autogenerated project paths that are created during build-time for Aspire host projects.
    //The string key is the full project path (.csproj) and the string value is the full project name (with namespace
    private Dictionary<string, string> _autoGeneratedProjectNames;
    public StorageCommand(IFileSystem fileSystem, ILogger logger, IEnvironmentService environmentService)
    {
        _environmentService = environmentService;
        _fileSystem = fileSystem;
        _logger = logger;
        _autoGeneratedProjectNames = [];
    }

    public override async Task<int> ExecuteAsync([NotNull] CommandContext context, [NotNull] StorageCommandSettings settings)
    {
        new MsBuildInitializer(_logger).Initialize();
        if (!ValidateStorageCommandSettings(settings))
        {
            return -1;
        }
        _logger.LogMessage("Installing packages...");
        await InstallPackagesAsync(settings);

        _logger.LogMessage("Updating App host project...");
        var appHostResult = await UpdateAppHostAsync(settings);

        _logger.LogMessage("Updating web/worker project...");
        var workerResult = await UpdateWebApiAsync(settings);

        if (appHostResult && workerResult)
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

    internal async Task<bool> UpdateAppHostAsync(StorageCommandSettings commandSettings)
    {
        CodeModifierConfig? config = ProjectModifierHelper.GetCodeModifierConfig("storage-apphost.json", System.Reflection.Assembly.GetExecutingAssembly());
        var workspaceSettings = new WorkspaceSettings
        {
            InputPath = commandSettings.AppHostProject
        };

        var hostAppSettings = new AppSettings();
        hostAppSettings.AddSettings("workspace", workspaceSettings);
        var codeService = new CodeService(hostAppSettings, _logger);
        //initialize _autoGeneratedProjectNames here. 
        await GetAutoGeneratedProjectNamesAsync(codeService, commandSettings.Project);
        CodeChangeOptions options = new()
        {
            IsMinimalApp = await ProjectModifierHelper.IsMinimalApp(codeService),
            UsingTopLevelsStatements = await ProjectModifierHelper.IsUsingTopLevelStatements(codeService)
        };

        //edit CodeModifierConfig to add the web project name from _autoGeneratedProjectNames.
        _autoGeneratedProjectNames.TryGetValue(commandSettings.Project, out var autoGenProjectName);
        config = EditConfigForAppHost(config, options, autoGenProjectName, commandSettings.Type);

        var projectModifier = new ProjectModifier(
            _environmentService,
            hostAppSettings,
            codeService,
            _logger,
            options,
            config);
        return await projectModifier.RunAsync();
    }

    internal async Task GetAutoGeneratedProjectNamesAsync(CodeService codeService, string projectPath)
    {
        var allDocuments = await codeService.GetAllDocumentsAsync();
        var allSyntaxRoots = await Task.WhenAll(allDocuments.Select(doc => doc.GetSyntaxRootAsync()));

        // Get all classes with the "Projects" namespace
        var classesInNamespace = allSyntaxRoots
            .SelectMany(root => root?.DescendantNodes().OfType<ClassDeclarationSyntax>() ?? Enumerable.Empty<ClassDeclarationSyntax>())
            .Where(cls => cls.IsInNamespace("Projects"))
            .ToList();

        foreach (var classSyntax in classesInNamespace)
        {
            string? projectPathValue = classSyntax.GetStringPropertyValue("ProjectPath");
            // Get the full class name including the namespace
            var className = classSyntax.Identifier.Text;
            if (!string.IsNullOrEmpty(projectPathValue))
            {
                _autoGeneratedProjectNames.Add(projectPathValue, $"Projects.{className}");
            }
        }
    }

    internal async Task<bool> UpdateWebApiAsync(StorageCommandSettings commandSettings)
    {
        CodeModifierConfig? config = ProjectModifierHelper.GetCodeModifierConfig("storage-webapi.json", System.Reflection.Assembly.GetExecutingAssembly());
        var workspaceSettings = new WorkspaceSettings
        {
            InputPath = commandSettings.Project
        };

        var webApiSettings = new AppSettings();
        webApiSettings.AddSettings("workspace", workspaceSettings);
        var codeService = new CodeService(webApiSettings, _logger);

        CodeChangeOptions options = new()
        {
            IsMinimalApp = await ProjectModifierHelper.IsMinimalApp(codeService),
            UsingTopLevelsStatements = await ProjectModifierHelper.IsUsingTopLevelStatements(codeService)
        };

        config = EditConfigForApiService(config, options, commandSettings.Type);
        var projectModifier = new ProjectModifier(
            _environmentService,
            webApiSettings,
            codeService,
            _logger,
            options,
            config);
        return await projectModifier.RunAsync();
    }

    internal CodeModifierConfig? EditConfigForAppHost(CodeModifierConfig? configToEdit, CodeChangeOptions codeChangeOptions, string? projectName, string storageType)
    {
        if (configToEdit is null)
        {
            return null;
        }

        var programCsFile = configToEdit.Files?.FirstOrDefault(x => !string.IsNullOrEmpty(x.FileName) && x.FileName.Equals("Program.cs", StringComparison.OrdinalIgnoreCase));
        if (programCsFile is not null &&
            programCsFile.Methods is not null &&
            programCsFile.Methods.Count != 0 &&
            StorageConstants.StoragePropertiesDict.TryGetValue(storageType, out var storageProperties))
        {
            var globalMethod = programCsFile.Methods.Where(x => x.Key.Equals("Global", StringComparison.OrdinalIgnoreCase)).First().Value;
            //to inject correct storage variables in 'var {0} = storage.{1}("{0}")'
            var addAzureStorageChange = globalMethod?.CodeChanges?.FirstOrDefault(x => !string.IsNullOrEmpty(x.InsertAfter) && x.InsertAfter.Contains("builder.AddAzureStorage"));
            var addProjectChange = globalMethod?.CodeChanges?.FirstOrDefault(x => !string.IsNullOrEmpty(x.Parent) && x.Parent.Contains("builder.AddProject<{0}>"));
            //apply changes to addAzureStorageCommand
            if (!codeChangeOptions.UsingTopLevelsStatements && addAzureStorageChange is not null)
            {
                addAzureStorageChange = DocumentBuilder.AddLeadingTriviaSpaces(addAzureStorageChange, spaces: 12);
            }

            if (addAzureStorageChange is not null && !string.IsNullOrEmpty(addAzureStorageChange.Block) && storageProperties is not null)
            {
                //update the parent value with the project name inserted.
                addAzureStorageChange.Block = string.Format(addAzureStorageChange.Block, storageProperties.VariableName, storageProperties.AddMethodName);
            }

            //apply changes to addProjectChange
            if (!codeChangeOptions.UsingTopLevelsStatements && addProjectChange is not null)
            {
                addProjectChange = DocumentBuilder.AddLeadingTriviaSpaces(addProjectChange, spaces: 12);
            }

            if (addProjectChange is not null &&
                storageProperties is not null &&
                !string.IsNullOrEmpty(addProjectChange.Parent) &&
                !string.IsNullOrEmpty(addProjectChange.Block))
            {
                //update the parent value with the project name inserted.
                addProjectChange.Parent = string.Format(addProjectChange.Parent, projectName);
                addProjectChange.Block = string.Format(addProjectChange.Block, storageProperties.VariableName);
            }
        }

        return configToEdit;
    }

    internal CodeModifierConfig? EditConfigForApiService(CodeModifierConfig? configToEdit, CodeChangeOptions codeChangeOptions, string storageType)
    {
        if (configToEdit is null)
        {
            return null;
        }

        var programCsFile = configToEdit.Files?.FirstOrDefault(x => !string.IsNullOrEmpty(x.FileName) && x.FileName.Equals("Program.cs", StringComparison.OrdinalIgnoreCase));
        if (programCsFile is not null && programCsFile.Methods is not null && programCsFile.Methods.Count != 0 && StorageConstants.StoragePropertiesDict.TryGetValue(storageType, out var storageProperties))
        {
            var globalMethod = programCsFile.Methods.Where(x => x.Key.Equals("Global", StringComparison.OrdinalIgnoreCase)).First().Value;
            //only one change in here
            var addClientChange = globalMethod?.CodeChanges?.FirstOrDefault();
            if (addClientChange is not null && storageProperties is not null)
            {
                addClientChange.Block = string.Format(addClientChange.Block, storageProperties.VariableName, storageProperties.AddClientMethodName);
            }
        }

        return configToEdit;
    }

    internal bool ValidateStorageCommandSettings(StorageCommandSettings commandSettings)
    {
        if (string.IsNullOrEmpty(commandSettings.Type) || !GetCmdsHelper.StorageTypeCustomValues.Contains(commandSettings.Type, StringComparer.OrdinalIgnoreCase))
        {
            string storageTypeDisplayList = string.Join(", ", GetCmdsHelper.StorageTypeCustomValues.GetRange(0, GetCmdsHelper.StorageTypeCustomValues.Count - 1)) +
                (GetCmdsHelper.StorageTypeCustomValues.Count > 1 ? " and " : "") + GetCmdsHelper.StorageTypeCustomValues[GetCmdsHelper.StorageTypeCustomValues.Count - 1];
            _logger.LogMessage("Missing/Invalid --type option.", LogMessageType.Error);
            _logger.LogMessage($"Valid options : {storageTypeDisplayList}", LogMessageType.Error);
            return false;
        }

        if (string.IsNullOrEmpty(commandSettings.AppHostProject))
        {
            _logger.LogMessage("Missing/Invalid --apphost-project option.", LogMessageType.Error);
            return false;
        }

        if (string.IsNullOrEmpty(commandSettings.Project))
        {
            _logger.LogMessage("Missing/Invalid --project option.", LogMessageType.Error);
            return false;
        }

        return true;
    }

    internal async Task InstallPackagesAsync(StorageCommandSettings commandSettings)
    {
        var appHostPackageStep= new AddPackagesStep
        {
            PackageNames = [PackageConstants.StoragePackages.AppHostStoragePackageName],
            ProjectPath = commandSettings.AppHostProject,
            Prerelease = commandSettings.Prerelease,
            Logger = _logger
        };

        PackageConstants.StoragePackages.StoragePackagesDict.TryGetValue(commandSettings.Type, out string? projectPackageName);
        var workerProjPackageStep= new AddPackagesStep
        {
            PackageNames = [projectPackageName],
            ProjectPath = commandSettings.AppHostProject,
            Prerelease = commandSettings.Prerelease,
            Logger = _logger
        };

        List<AddPackagesStep> packageSteps = [appHostPackageStep, workerProjPackageStep];
        foreach (var packageStep in packageSteps)
        {
            await packageStep.ExecuteAsync();
        }
    }

    public class StorageCommandSettings : CommandSettings
    {
        [CommandOption("--type <TYPE>")]
        public required string Type { get; set; }

        [CommandOption("--apphost-project <APPHOSTPROJECT>")]
        public required string AppHostProject { get; set; }

        [CommandOption("--project <PROJECT>")]
        public required string Project { get; set; }

        [CommandOption("--prerelease")]
        public required bool Prerelease { get; set; }
    }
}
