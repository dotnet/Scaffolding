// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using Microsoft.DotNet.Scaffolding.Core.Scaffolders;
using Microsoft.DotNet.Scaffolding.Core.Steps;
using Microsoft.DotNet.Scaffolding.Internal.Services;
using Microsoft.DotNet.Scaffolding.TextTemplating;
using Microsoft.Extensions.Logging;

namespace Microsoft.DotNet.Tools.Scaffold.AspNet.ScaffoldSteps;

internal class AddFileStep : ScaffoldStep
{
    //this file name should be found in dotnet-scaffold-aspnet\Templates\Files
    public required string FileName { get; set; }
    public required string BaseOutputDirectory { get; set; }
    private readonly ILogger _logger;
    private readonly IFileSystem _fileSystem;

    public AddFileStep(ILogger<AddFileStep> logger, IFileSystem fileSystem)
    {
        _logger = logger;
        _fileSystem = fileSystem;
    }

    public override Task<bool> ExecuteAsync(ScaffolderContext context, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(BaseOutputDirectory) ||
            string.IsNullOrEmpty(FileName))
        {
            return Task.FromResult(false);
        }

        var destinationFilePath = Path.Combine(BaseOutputDirectory, FileName);
        if (File.Exists(destinationFilePath))
        {
            return Task.FromResult(false);
        }

        var allFiles = new TemplateFoldersUtilities().GetAllFiles(["Files"]);
        var fileToCopy = allFiles.FirstOrDefault(x => x.EndsWith(FileName, StringComparison.OrdinalIgnoreCase));
        var destinationDirectory = Path.GetDirectoryName(destinationFilePath);
        if (!string.IsNullOrEmpty(fileToCopy) && !string.IsNullOrEmpty(destinationDirectory))
        {
            _logger.LogInformation($"Adding file '{FileName}'...");

            if (!_fileSystem.DirectoryExists(destinationDirectory))
            {
                _fileSystem.CreateDirectory(destinationDirectory);
            }

            _fileSystem.CopyFile(fileToCopy, destinationFilePath, overwrite: false);
            _logger.LogInformation("Done");
            return Task.FromResult(true);
        }

        return Task.FromResult(false);
    }
}
