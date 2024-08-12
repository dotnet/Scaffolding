// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using Microsoft.DotNet.Scaffolding.Core.Scaffolders;
using Microsoft.DotNet.Scaffolding.Core.Steps;
using Microsoft.DotNet.Scaffolding.Internal.Services;
using Microsoft.DotNet.Tools.Scaffold.AspNet.ScaffoldSteps.Settings;
using Microsoft.Extensions.Logging;

namespace Microsoft.DotNet.Tools.Scaffold.AspNet.ScaffoldSteps;

internal class AreaScaffolderStep : ScaffoldStep
{
    public string? Project { get; set; }
    public string? Name { get; set; }
    private readonly ILogger _logger;
    private readonly IFileSystem _fileSystem;
    private readonly IEnvironmentService _environmentService;
    public AreaScaffolderStep(IFileSystem fileSystem, ILogger<AreaScaffolderStep> logger, IEnvironmentService environmentService)
    {
        _environmentService = environmentService;
        _fileSystem = fileSystem;
        _logger = logger;
    }

    private AreaStepSettings? ValidateAreaCommandSettings()
    {
        if (string.IsNullOrEmpty(Name))
        {
            _logger.LogError("Missing/Invalid --name option.");
            return null;
        }

        if (string.IsNullOrEmpty(Project) || !_fileSystem.FileExists(Project))
        {
            _logger.LogInformation("Missing/Invalid --project option.");
            return null;
        }

        return new AreaStepSettings
        {
            Project = Project,
            Name = Name,
        };
    }

    private void EnsureFolderLayout(AreaStepSettings stepSettings)
    {
        _logger.LogInformation($"Adding area '{stepSettings.Name}'...");
        var basePath = _environmentService.CurrentDirectory;
        var projectDirectoryPath = Path.GetDirectoryName(stepSettings.Project);
        if (!string.IsNullOrEmpty(projectDirectoryPath) && _fileSystem.DirectoryExists(projectDirectoryPath))
        {
            basePath = projectDirectoryPath;
        }

        var areaBasePath = Path.Combine(basePath, "Areas");
        if (!_fileSystem.DirectoryExists(areaBasePath))
        {
            _fileSystem.CreateDirectory(areaBasePath);
        }

        var areaPath = Path.Combine(areaBasePath, stepSettings.Name);
        if (!_fileSystem.DirectoryExists(areaPath))
        {
            _fileSystem.CreateDirectory(areaPath);
        }

        foreach (var areaFolder in AreaFolders)
        {
            var path = Path.Combine(areaPath, areaFolder);
            if (!_fileSystem.DirectoryExists(path))
            {
                _fileSystem.CreateDirectory(path);
            }
        }

        _logger.LogInformation("Done");
    }

    public override Task<bool> ExecuteAsync(ScaffolderContext context, CancellationToken cancellationToken = default)
    {
        var stepSettings = ValidateAreaCommandSettings();
        if (stepSettings is null)
        {
            return Task.FromResult(false);
        }

        _logger.LogInformation("Updating project...");
        EnsureFolderLayout(stepSettings);
        _logger.LogInformation("Finished");
        return Task.FromResult(true);
    }

    private static readonly string[] AreaFolders =
    [
        "Controllers",
        "Models",
        "Data",
        "Views"
    ];
}
