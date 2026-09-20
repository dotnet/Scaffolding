// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using System.Text.Json;
using Microsoft.DotNet.Scaffolding.Core.Scaffolders;
using Microsoft.DotNet.Scaffolding.Core.Steps;
using Microsoft.DotNet.Scaffolding.Internal.CliHelpers;
using Microsoft.DotNet.Scaffolding.Internal.Services;
using Microsoft.Extensions.Logging;

namespace Microsoft.DotNet.Tools.Scaffold.AspNet.ScaffoldSteps;

/// <summary>
/// Generates an initial EF Core migration for a newly configured Identity context.
/// </summary>
internal class AddIdentityMigrationStep(
    ILogger<AddIdentityMigrationStep> logger,
    IFileSystem fileSystem) : ScaffoldStep
{
    public required string ProjectPath { get; set; }
    public required string DbContextName { get; set; }
    public required string ProjectAssetsFile { get; set; }

    public override Task<bool> ExecuteAsync(ScaffolderContext context, CancellationToken cancellationToken = default)
    {
        var projectDirectory = Path.GetDirectoryName(ProjectPath);
        if (string.IsNullOrEmpty(projectDirectory) || string.IsNullOrEmpty(DbContextName))
        {
            logger.LogError("Unable to determine the project or DbContext while generating the Identity migration.");
            return Task.FromResult(false);
        }

        if (string.IsNullOrEmpty(ProjectAssetsFile) || !fileSystem.FileExists(ProjectAssetsFile))
        {
            logger.LogError($"Unable to generate the Identity migration because the project's assets file '{ProjectAssetsFile}' does not exist.");
            return Task.FromResult(false);
        }

        var efVersion = GetEfDesignPackageVersion(fileSystem.ReadAllText(ProjectAssetsFile));
        if (string.IsNullOrEmpty(efVersion))
        {
            logger.LogError("Unable to determine the Microsoft.EntityFrameworkCore.Design package version.");
            return Task.FromResult(false);
        }

        var toolDirectory = Path.Combine(fileSystem.GetTempPath(), "dotnet-scaffold", Guid.NewGuid().ToString("N"));
        try
        {
            fileSystem.CreateDirectoryIfNotExists(toolDirectory);
            if (!InstallEfTool(toolDirectory, projectDirectory, efVersion))
            {
                return Task.FromResult(false);
            }

            return Task.FromResult(AddMigration(toolDirectory, projectDirectory));
        }
        finally
        {
            try
            {
                Directory.Delete(toolDirectory, recursive: true);
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                logger.LogWarning($"Unable to remove temporary EF Core tooling directory '{toolDirectory}': {ex.Message}");
            }
        }
    }

    internal static string? GetEfDesignPackageVersion(string assetsContent)
    {
        using var document = JsonDocument.Parse(assetsContent);
        if (!document.RootElement.TryGetProperty("libraries", out var libraries))
        {
            return null;
        }

        const string packagePrefix = "Microsoft.EntityFrameworkCore.Design/";
        foreach (var library in libraries.EnumerateObject())
        {
            if (library.Name.StartsWith(packagePrefix, StringComparison.OrdinalIgnoreCase))
            {
                return library.Name[packagePrefix.Length..];
            }
        }

        return null;
    }

    private bool InstallEfTool(string toolDirectory, string projectDirectory, string version)
    {
        logger.LogInformation("Installing temporary EF Core tooling...");
        var runner = DotnetCliRunner.CreateDotNet("tool",
        [
            "install",
            "dotnet-ef",
            "--tool-path",
            toolDirectory,
            "--version",
            version
        ]);
        runner._psi.WorkingDirectory = projectDirectory;
        var exitCode = runner.ExecuteAndCaptureOutput(out var stdOut, out var stdErr);
        if (exitCode == 0)
        {
            return true;
        }

        logger.LogError($"Unable to install dotnet-ef {version}.{Environment.NewLine}{stdOut}{Environment.NewLine}{stdErr}");
        return false;
    }

    private bool AddMigration(string toolDirectory, string projectDirectory)
    {
        logger.LogInformation("Generating initial Identity migration...");
        var executableName = OperatingSystem.IsWindows() ? "dotnet-ef.exe" : "dotnet-ef";
        var runner = DotnetCliRunner.Create(Path.Combine(toolDirectory, executableName),
        [
            "migrations",
            "add",
            "CreateIdentitySchema",
            "--project",
            ProjectPath,
            "--startup-project",
            ProjectPath,
            "--context",
            DbContextName,
            "--output-dir",
            Path.Combine("Data", "Migrations"),
            "--no-color"
        ]);
        runner._psi.WorkingDirectory = projectDirectory;
        var exitCode = runner.ExecuteAndCaptureOutput(out var stdOut, out var stdErr);
        if (exitCode == 0)
        {
            logger.LogInformation("Done");
            return true;
        }

        logger.LogError($"Unable to generate the initial Identity migration.{Environment.NewLine}{stdOut}{Environment.NewLine}{stdErr}");
        return false;
    }
}
