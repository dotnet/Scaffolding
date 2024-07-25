// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using Microsoft.DotNet.Scaffolding.Core.Scaffolders;
using Microsoft.DotNet.Scaffolding.Helpers.Services;
using Microsoft.DotNet.Scaffolding.Helpers.Services.Environment;
using Microsoft.DotNet.Tools.Scaffold.AspNet.Commands.Settings;
using Microsoft.Extensions.Logging;

namespace Microsoft.DotNet.Tools.Scaffold.AspNet.Commands;

internal class AreaCommand : ICommandWithSettings<AreaCommandSettings>
{
    private readonly ILogger _logger;
    private readonly IFileSystem _fileSystem;
    private readonly IEnvironmentService _environmentService;
    public AreaCommand(IFileSystem fileSystem, ILogger logger, IEnvironmentService environmentService)
    {
        _environmentService = environmentService;
        _fileSystem = fileSystem;
        _logger = logger;
    }

    public Task<int> ExecuteAsync(AreaCommandSettings settings, ScaffolderContext context)
    {
        if (!ValidateAreaCommandSettings(settings))
        {
            return Task.FromResult(-1);
        }

        _logger.LogInformation("Updating project...");
        EnsureFolderLayout(settings);
        _logger.LogInformation("Finished");
        return Task.FromResult(0);
    }

    private bool ValidateAreaCommandSettings(AreaCommandSettings commandSettings)
    {
        if (string.IsNullOrEmpty(commandSettings.Name))
        {
            _logger.LogError("Missing/Invalid --name option.");
            return false;
        }

        return true;
    }

    private void EnsureFolderLayout(AreaCommandSettings commandSettings)
    {
        var basePath = _environmentService.CurrentDirectory;
        var projectDirectoryPath = Path.GetDirectoryName(commandSettings.Project);
        if (!string.IsNullOrEmpty(projectDirectoryPath) && _fileSystem.DirectoryExists(projectDirectoryPath))
        {
            basePath = projectDirectoryPath;
        }

        var areaBasePath = Path.Combine(basePath, "Areas");
        if (!_fileSystem.DirectoryExists(areaBasePath))
        {
            _fileSystem.CreateDirectory(areaBasePath);
        }

        var areaPath = Path.Combine(areaBasePath, commandSettings.Name);
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
    }


    private static readonly string[] AreaFolders =
    [
        "Controllers",
        "Models",
        "Data",
        "Views"
    ];
}
