// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using System.Text;
using Microsoft.DotNet.Scaffolding.Core.Scaffolders;
using Microsoft.DotNet.Scaffolding.Core.Steps;
using Microsoft.DotNet.Scaffolding.Internal.Services;
using Microsoft.DotNet.Scaffolding.TextTemplating;
using Microsoft.Extensions.Logging;

namespace Microsoft.DotNet.Tools.Scaffold.AspNet.ScaffoldSteps;

/// <summary>
/// Scaffold step for adding a file from the template folder to the output directory if it does not already exist.
/// </summary>
internal class AddFileStep : ScaffoldStep
{
    //this file name should be found in dotnet-scaffold-aspnet\Templates\Files
    /// <summary>
    /// Gets or sets the file name to add (should exist in the template folder).
    /// </summary>
    public required string FileName { get; set; }

    /// <summary>
    /// Gets or sets the project file path where the file will be added.
    /// </summary>
    public required string ProjectPath {get; set; }

    /// <summary>
    /// Gets or sets the base output directory where the file will be added.
    /// </summary>
    public required string BaseOutputDirectory { get; set; }

    /// <summary>
    /// Gets or sets whether an existing file should be overwritten.
    /// </summary>
    public bool Overwrite { get; set; }

    private readonly ILogger _logger;
    private readonly IFileSystem _fileSystem;

    /// <summary>
    /// Initializes a new instance of the <see cref="AddFileStep"/> class.
    /// </summary>
    public AddFileStep(ILogger<AddFileStep> logger, IFileSystem fileSystem)
    {
        _logger = logger;
        _fileSystem = fileSystem;
    }

    /// <inheritdoc />
    public override Task<bool> ExecuteAsync(ScaffolderContext context, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(BaseOutputDirectory) ||
            string.IsNullOrEmpty(FileName))
        {
            _logger.LogError("A file name and output directory are required to add a template file.");
            return Task.FromResult(false);
        }

        var destinationFilePath = Path.Combine(BaseOutputDirectory, FileName);
        var allFiles = new TemplateFoldersUtilities().GetAllFilesForTargetFramework(["Files"], ProjectPath);
        var fileToCopy = allFiles.FirstOrDefault(x => x.EndsWith(FileName, StringComparison.OrdinalIgnoreCase));
        var destinationDirectory = Path.GetDirectoryName(destinationFilePath);
        if (!string.IsNullOrEmpty(fileToCopy) && !string.IsNullOrEmpty(destinationDirectory))
        {
            if (_fileSystem.FileExists(destinationFilePath) && !Overwrite)
            {
                using var source = _fileSystem.OpenFileStream(fileToCopy, FileMode.Open, FileAccess.Read, FileShare.Read);
                using var destination = _fileSystem.OpenFileStream(destinationFilePath, FileMode.Open, FileAccess.Read, FileShare.Read);
                if (!FilesMatch(source, destination))
                {
                    _logger.LogError("Existing file '{FileName}' differs from the template. It was not overwritten.", destinationFilePath);
                    return Task.FromResult(false);
                }

                _logger.LogInformation("File '{FileName}' already matches the template.", destinationFilePath);
                return Task.FromResult(true);
            }

            _logger.LogInformation($"Adding file '{FileName}'...");

            _fileSystem.CreateDirectoryIfNotExists(destinationDirectory);
            _fileSystem.CopyFile(fileToCopy, destinationFilePath, Overwrite);
            _logger.LogInformation("Done");
            return Task.FromResult(true);
        }

        _logger.LogError("Unable to find template '{FileName}' or its destination directory for project '{ProjectPath}'.", FileName, ProjectPath);
        return Task.FromResult(false);
    }

    private static bool FilesMatch(Stream source, Stream destination)
    {
        try
        {
            using var sourceReader = new StreamReader(source, new UTF8Encoding(false, true), detectEncodingFromByteOrderMarks: true);
            using var destinationReader = new StreamReader(destination, new UTF8Encoding(false, true), detectEncodingFromByteOrderMarks: true);
            int nextCharacter;
            while ((nextCharacter = sourceReader.Read()) != -1)
            {
                if (nextCharacter != destinationReader.Read())
                {
                    return false;
                }
            }

            return destinationReader.Read() == -1;
        }
        catch (DecoderFallbackException)
        {
            // Undecodable text is a conflict; the caller logs it and preserves the existing file.
            return false;
        }
    }
}
