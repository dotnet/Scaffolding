// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json;
using Microsoft.DotNet.Scaffolding.Internal.Services;
using Microsoft.DotNet.Tools.Scaffold.Models;

namespace Microsoft.DotNet.Tools.Scaffold.Services;

/// <summary>
/// Service for managing the dotnet-scaffold tool manifest (tools.json).
/// Handles adding, removing, and retrieving scaffold tools from the manifest file.
/// </summary>
internal class ToolManifestService(IFileSystem fileSystem) : IToolManifestService
{
    // The current version of the manifest schema.
    private static readonly string CURRENT_MANIFEST_VERSION = "0.1";

    // File system abstraction for reading/writing files and directories.
    private readonly IFileSystem _fileSystem = fileSystem;

    /// <summary>
    /// Adds a tool to the manifest if it does not already exist.
    /// </summary>
    /// <param name="toolName">The name of the tool to add.</param>
    /// <returns>True if the tool was added or already exists.</returns>
    public bool AddTool(string toolName)
    {
        var currentManifest = GetManifest();
        if (!currentManifest.HasTool(toolName))
        {
            currentManifest.Tools.Add(new ScaffoldTool { Name = toolName });
            WriteManifest(currentManifest);
        }
        
        return true;
    }

    /// <summary>
    /// Ensures the manifest directory exists, creating it if necessary.
    /// </summary>
    private void EnsureManifestDirectory()
    {
        _fileSystem.CreateDirectoryIfNotExists(GetToolManifestDirectory());
    }

    /// <summary>
    /// Retrieves the current scaffold manifest, creating a default one if it does not exist.
    /// </summary>
    /// <returns>The current <see cref="ScaffoldManifest"/>.</returns>
    public ScaffoldManifest GetManifest()
    {
        EnsureManifestDirectory();
        if (_fileSystem.FileExists(GetToolManifestPath()))
        {
            var manifestContent = _fileSystem.ReadAllText(GetToolManifestPath());
            var manifest = JsonSerializer.Deserialize<ScaffoldManifest>(manifestContent);

            if (manifest is not null)
            {
                return manifest;
            }
        }

        return CreateDefaultManifest();
    }

    /// <summary>
    /// Removes a tool from the manifest by name.
    /// </summary>
    /// <param name="toolName">The name of the tool to remove.</param>
    /// <returns>True if the tool was removed; false if not found.</returns>
    public bool RemoveTool(string toolName)
    {
        var currentManifest = GetManifest();
        var toolToRemove = currentManifest.Tools.FirstOrDefault(t => string.Equals(toolName, t.Name, StringComparison.OrdinalIgnoreCase));
        if (toolToRemove is not null)
        {
            currentManifest.Tools.Remove(toolToRemove);
            WriteManifest(currentManifest);
            return true;
        }
        else
        {
            return false;
        }
    }

    /// <summary>
    /// Writes the manifest to disk as JSON.
    /// </summary>
    /// <param name="manifest">The manifest to write.</param>
    private void WriteManifest(ScaffoldManifest manifest)
    {
        EnsureManifestDirectory();
        var json = JsonSerializer.Serialize(manifest);
        _fileSystem.WriteAllText(GetToolManifestPath(), json);
    }

    /// <summary>
    /// Gets the directory path for the tool manifest.
    /// </summary>
    private static string GetToolManifestDirectory()
        => Path.Combine(Environment.GetEnvironmentVariable("USERPROFILE") ?? "", ".dotnet-scaffold");

    /// <summary>
    /// Gets the file path for the tool manifest (tools.json).
    /// </summary>
    private static string GetToolManifestPath()
        => Path.Combine(GetToolManifestDirectory(), "tools.json");

    /// <summary>
    /// Creates a default scaffold manifest with the current version and no tools.
    /// </summary>
    private static ScaffoldManifest CreateDefaultManifest() => new() { Version = CURRENT_MANIFEST_VERSION, Tools = [] };
}
