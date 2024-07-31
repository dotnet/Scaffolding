// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json;
using Microsoft.DotNet.Scaffolding.Internal.Services;
using Microsoft.DotNet.Tools.Scaffold.Models;

namespace Microsoft.DotNet.Tools.Scaffold.Services;

internal class ToolManifestService(IFileSystem fileSystem) : IToolManifestService
{
    private static readonly string CURRENT_MANIFEST_VERSION = "0.1";

    private readonly IFileSystem _fileSystem = fileSystem;

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

    private void EnsureManifestDirectory()
    {
        if (!_fileSystem.DirectoryExists(GetToolManifestDirectory()))
        {
            _fileSystem.CreateDirectory(GetToolManifestDirectory());
        }
    }

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

    private void WriteManifest(ScaffoldManifest manifest)
    {
        EnsureManifestDirectory();
        var json = JsonSerializer.Serialize(manifest);
        _fileSystem.WriteAllText(GetToolManifestPath(), json);
    }

    private static string GetToolManifestDirectory()
    => Path.Combine(Environment.GetEnvironmentVariable("USERPROFILE") ?? "", ".dotnet-scaffold");
    private static string GetToolManifestPath()
        => Path.Combine(GetToolManifestDirectory(), "tools.json");

    private static ScaffoldManifest CreateDefaultManifest() => new() { Version = CURRENT_MANIFEST_VERSION, Tools = [] };
}
