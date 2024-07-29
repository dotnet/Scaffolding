// Licensed to the .NET Foundation under one or more agreements.
using System.Globalization;
using Microsoft.DotNet.Scaffolding.Core;
using Microsoft.DotNet.Scaffolding.Core.Scaffolders;
using Microsoft.DotNet.Scaffolding.Core.Services;
using Microsoft.DotNet.Tools.Scaffold.AspNet.Commands.Settings;
using Microsoft.Extensions.Logging;

namespace Microsoft.DotNet.Tools.Scaffold.AspNet.Commands;

internal class EmptyControllerCommand : ICommandWithSettings<EmptyControllerCommandSettings>
{
    private readonly ILogger _logger;
    private readonly IFileSystem _fileSystem;
    public EmptyControllerCommand(IFileSystem fileSystem, ILogger logger)
    {
        _fileSystem = fileSystem;
        _logger = logger;
    }

    public Task<int> ExecuteAsync(EmptyControllerCommandSettings commandSettings, ScaffolderContext context)
    {
        if (!ValidateEmptyControllerCommandSettings(commandSettings))
        {
            return Task.FromResult(-1);
        }

        _logger.LogInformation($"Adding '{commandSettings.CommandName}'...");
        var addingResult = InvokeDotnetNew(commandSettings);

        if (addingResult)
        {
            _logger.LogInformation("Finished");
            return Task.FromResult(0);
        }
        else
        {
            _logger.LogError("An error occurred.");
            return Task.FromResult(-1);
        }
    }

    private bool InvokeDotnetNew(EmptyControllerCommandSettings settings)
    {
        var projectBasePath = Path.GetDirectoryName(settings.Project);
        var actionsParameter = settings.Actions ? "--actions" : string.Empty;
        if (Directory.Exists(projectBasePath))
        {
            //arguments for 'dotnet new {settings.CommandName}'
            var args = new List<string>()
            {
                settings.CommandName,
                "--name",
                settings.Name,
                "--actions",
                "--output",
                projectBasePath,
                actionsParameter
            };

            var runner = DotnetCliRunner.CreateDotNet("new", args);
            var exitCode = runner.ExecuteAndCaptureOutput(out _, out _);
            return exitCode == 0;
        }

        return false;
    }

    private bool ValidateEmptyControllerCommandSettings(EmptyControllerCommandSettings commandSettings)
    {
        if (string.IsNullOrEmpty(commandSettings.Project) || !_fileSystem.FileExists(commandSettings.Project))
        {
            _logger.LogInformation("Missing/Invalid --project option.");
            return false;
        }

        if (string.IsNullOrEmpty(commandSettings.Name))
        {
            _logger.LogInformation("Missing/Invalid --name option.");
            return false;
        }
        else
        {
            //Component names cannot start with a lowercase character, using CurrentCulture to capitalize the first letter
            commandSettings.Name = CultureInfo.CurrentCulture.TextInfo.ToTitleCase(commandSettings.Name);
        }

        return true;
    }
}
