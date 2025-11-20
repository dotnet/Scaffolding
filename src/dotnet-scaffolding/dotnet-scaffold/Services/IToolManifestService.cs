// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.DotNet.Tools.Scaffold.Models;

namespace Microsoft.DotNet.Tools.Scaffold.Services;

/// <summary>
/// Defines operations for managing the dotnet-scaffold tool manifest.
/// </summary>
internal interface IToolManifestService
{
    /// <summary>
    /// Adds a tool to the manifest if it does not already exist.
    /// </summary>
    /// <param name="toolName">The name of the tool to add.</param>
    /// <returns>True if the tool was added or already exists.</returns>
    bool AddTool(string toolName);

    /// <summary>
    /// Retrieves the current scaffold manifest, creating a default one if it does not exist.
    /// </summary>
    /// <returns>The current <see cref="ScaffoldManifest"/>.</returns>
    ScaffoldManifest GetManifest();

    /// <summary>
    /// Removes a tool from the manifest by name.
    /// </summary>
    /// <param name="toolName">The name of the tool to remove.</param>
    /// <returns>True if the tool was removed; false if not found.</returns>
    bool RemoveTool(string toolName);
}
