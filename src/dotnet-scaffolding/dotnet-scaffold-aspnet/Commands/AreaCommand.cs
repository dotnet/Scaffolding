// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using Spectre.Console.Cli;
using System.IO;
using System.Diagnostics.CodeAnalysis;
using Microsoft.DotNet.Scaffolding.Helpers.Services.Environment;
using Microsoft.DotNet.Scaffolding.Helpers.Services;

namespace Microsoft.DotNet.Tools.Scaffold.AspNet.Commands;

internal class AreaCommand : Command<AreaCommand.AreaSettings>
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

    public override int Execute([NotNull] CommandContext context, [NotNull] AreaSettings settings)
    {
        if (!ValidateAreaCommandSettings(settings))
        {
            return -1;
        }

        _logger.LogMessage("Updating project...");
        EnsureFolderLayout(settings);
        _logger.LogMessage("Finished");
        return 0;
    }

    private bool ValidateAreaCommandSettings(AreaSettings commandSettings)
    {
        if (string.IsNullOrEmpty(commandSettings.Name))
        {
            _logger.LogMessage("Missing/Invalid --name option.", LogMessageType.Error);
            return false;
        }

        return true;
    }

    private void EnsureFolderLayout(AreaSettings commandSettings)
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

    public class AreaSettings : CommandSettings
    {
        [CommandOption("--project <PROJECT>")]
        public string? Project { get; set; }

        [CommandOption("--name <NAME>")]
        public required string Name { get; set; }
    }
}
