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
using static Microsoft.DotNet.Scaffolding.Internal.Constants;

namespace Microsoft.DotNet.Tools.Scaffold.AspNet.ScaffoldSteps;

//TODO: pull all the duplicate logic from all these 'Validation' ScaffolderSteps into a common one.
internal class ValidateBlazorIdentityStep : ScaffoldStep
{
    private readonly IFileSystem _fileSystem;
    private readonly ILogger _logger;
    public bool Overwrite { get; set; }
    public string? Project { get; set; }
    public bool Prerelease { get; set; }
    public string? DatabaseProvider { get; set; }
    public string? DataContext { get; set; }
    public ValidateBlazorIdentityStep(
        IFileSystem fileSystem,
        ILogger<ValidateBlazorIdentityStep> logger)
    {
        _fileSystem = fileSystem;
        _logger = logger;
    }

    public override async Task<bool> ExecuteAsync(ScaffolderContext context, CancellationToken cancellationToken = default)
    {
        var blazorIdentitySettings = ValidateBlazorIdentitySettings();
        var codeModifierProperties = new Dictionary<string, string>();
        if (blazorIdentitySettings is null)
        {
            return false;
        }
        else
        {
            context.Properties.Add(nameof(BlazorIdentitySettings), blazorIdentitySettings);
        }

        //initialize BlazorIdentityModel
        _logger.LogInformation("Initializing scaffolding model...");
        var blazorIdentityModel = await GetBlazorIdentityModelAsync(blazorIdentitySettings);
        if (blazorIdentityModel is null)
        {
            _logger.LogError("An error occurred.");
            return false;
        }
        else
        {
            context.Properties.Add(nameof(BlazorIdentityModel), blazorIdentityModel);
            codeModifierProperties.Add(CodeModifierPropertyConstants.BlazorIdentityNamespace, blazorIdentityModel.BlazorIdentityNamespace);
            codeModifierProperties.Add(CodeModifierPropertyConstants.UserClassNamespace, blazorIdentityModel.UserClassNamespace);
        }

        //Install packages and add a DbContext (if needed)
        if (blazorIdentityModel.DbContextInfo.EfScenario)
        {
            var dbContextProperties = AspNetDbContextHelper.GetDbContextProperties(blazorIdentitySettings.Project, blazorIdentityModel.DbContextInfo);
            if (dbContextProperties is not null)
            {
                dbContextProperties.IsIdentityDbContext = true;
                dbContextProperties.FullIdentityUserName = $"{blazorIdentityModel.UserClassNamespace}.{blazorIdentityModel.UserClassName}";
                context.Properties.Add(nameof(DbContextProperties), dbContextProperties);
            }

            var projectBasePath = Path.GetDirectoryName(blazorIdentitySettings.Project);
            if (!string.IsNullOrEmpty(projectBasePath))
            {
                context.Properties.Add(Scaffolding.Internal.Constants.StepConstants.BaseProjectPath, projectBasePath);
            }

            var dbCodeModifierProperties = AspNetDbContextHelper.GetDbContextCodeModifierProperties(blazorIdentityModel.DbContextInfo);
            foreach (var kvp in dbCodeModifierProperties)
            {
                codeModifierProperties.TryAdd(kvp.Key, kvp.Value);
            }
        }

        context.Properties.Add(Scaffolding.Internal.Constants.StepConstants.CodeModifierProperties, codeModifierProperties);
        return true;
    }

    private BlazorIdentitySettings? ValidateBlazorIdentitySettings()
    {
        if (string.IsNullOrEmpty(Project) || !_fileSystem.FileExists(Project))
        {
            _logger.LogError("Missing/Invalid --project option.");
            return null;
        }

        if (string.IsNullOrEmpty(DataContext))
        {
            _logger.LogError("Missing/Invalid --dataContext option.");
            return null;
        }
        else if (string.IsNullOrEmpty(DatabaseProvider) || !PackageConstants.EfConstants.IdentityEfPackagesDict.ContainsKey(DatabaseProvider))
        {
            DatabaseProvider = PackageConstants.EfConstants.SqlServer;
        }

        return new BlazorIdentitySettings
        {
            Project = Project,
            DataContext = DataContext,
            DatabaseProvider = DatabaseProvider,
            Prerelease = Prerelease,
            Overwrite = Overwrite
        };
    }

    private async Task<BlazorIdentityModel?> GetBlazorIdentityModelAsync(BlazorIdentitySettings settings)
    {
        var projectInfo = ClassAnalyzers.GetProjectInfo(settings.Project, _logger);
        var projectDirectory = Path.GetDirectoryName(projectInfo.ProjectPath);
        if (projectInfo is null || projectInfo.CodeService is null || string.IsNullOrEmpty(projectDirectory))
        {
            return null;
        }

        var allClasses = await projectInfo.CodeService.GetAllClassSymbolsAsync();
        //find DbContext info or create properties for a new one.
        var dbContextClassName = settings.DataContext;
        DbContextInfo dbContextInfo = new();

        if (!string.IsNullOrEmpty(dbContextClassName) && !string.IsNullOrEmpty(settings.DatabaseProvider))
        {
            var dbContextClassSymbol = allClasses.FirstOrDefault(x => x.Name.Equals(dbContextClassName, StringComparison.OrdinalIgnoreCase));
            dbContextInfo = ClassAnalyzers.GetIdentityDbContextInfo(settings.Project, dbContextClassSymbol, dbContextClassName, settings.DatabaseProvider);
            dbContextInfo.EfScenario = true;
        }

        string blazorIdentityNamespace = string.Empty;
        string userClassNamespace = string.Empty;
        var projectName = Path.GetFileNameWithoutExtension(settings.Project);
        if (!string.IsNullOrEmpty(projectName))
        {
            blazorIdentityNamespace = $"{projectName}.Components.Account";
            userClassNamespace = $"{projectName}.Data";
        }

        BlazorIdentityModel scaffoldingModel = new()
        {
            ProjectInfo = projectInfo,
            DbContextInfo = dbContextInfo,
            BlazorIdentityNamespace = blazorIdentityNamespace,
            UserClassName = Constants.Identity.UserClassName,
            UserClassNamespace = userClassNamespace,
            DbContextName = Constants.Identity.DbContextName,
            BaseOutputPath = projectDirectory,
            Overwrite = settings.Overwrite
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
