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

internal class ValidateRazorPagesStep : ScaffoldStep
{
    private readonly IFileSystem _fileSystem;
    private readonly ILogger _logger;
    public string? Project { get; set; }
    public bool Prerelease { get; set; }
    public string? DatabaseProvider { get; set; }
    public string? DataContext { get; set; }
    public string? Model { get; set; }
    public string? Page { get; set; }

    public ValidateRazorPagesStep(
        IFileSystem fileSystem,
        ILogger<ValidateRazorPagesStep> logger)
    {
        _fileSystem = fileSystem;
        _logger = logger;
    }

    public override async Task<bool> ExecuteAsync(ScaffolderContext context, CancellationToken cancellationToken = default)
    {
        var razorPagesSettings = ValidateRazorPagesSettings();
        var codeModifierProperties = new Dictionary<string, string>();
        if (razorPagesSettings is null)
        {
            return false;
        }
        else
        {
            context.Properties.Add(nameof(CrudSettings), razorPagesSettings);
        }

        //initialize RazorPageModel
        _logger.LogInformation("Initializing scaffolding model...");
        var razorPageModel = await GetRazorPageModelAsync(razorPagesSettings);
        if (razorPageModel is null)
        {
            _logger.LogError("An error occurred.");
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
        return true;
    }

    private CrudSettings? ValidateRazorPagesSettings()
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

        if (string.IsNullOrEmpty(Page))
        {
            _logger.LogError("Missing/Invalid --page option.");
            return null;
        }
        else if (!string.IsNullOrEmpty(Page) && !BlazorCrudHelper.CRUDPages.Contains(Page, StringComparer.OrdinalIgnoreCase))
        {
            //if an invalid page name, switch to just "CRUD" and scaffold all pages
            Page = BlazorCrudHelper.CrudPageType;
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

    private async Task<RazorPageModel?> GetRazorPageModelAsync(CrudSettings settings)
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
