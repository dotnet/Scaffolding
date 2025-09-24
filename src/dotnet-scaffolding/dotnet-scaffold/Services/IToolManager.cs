// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.DotNet.Tools.Scaffold.Services;

/// <summary>
/// Defines operations for managing scaffold tools, including installation, removal, and listing.
/// </summary>
internal interface IToolManager
{
    /// <summary>
    /// Installs a scaffold tool and adds it to the manifest.
    /// </summary>
    /// <param name="packageName">The name of the tool package to install.</param>
    /// <param name="addSources">Additional sources for tool installation.</param>
    /// <param name="configFile">Optional NuGet config file path.</param>
    /// <param name="prerelease">Whether to allow prerelease versions.</param>
    /// <param name="version">Optional version to install.</param>
    /// <param name="global">Whether to install the tool globally.</param>
    /// <returns>True if the tool was installed and added to the manifest; otherwise, false.</returns>
    Task<bool> AddToolAsync(string packageName, string[] addSources, string? configFile, bool prerelease, string? version, bool global);

    /// <summary>
    /// Removes a scaffold tool from the manifest and uninstalls it.
    /// </summary>
    /// <param name="packageName">The name of the tool package to remove.</param>
    /// <param name="global">Whether the tool is installed globally.</param>
    /// <returns>True if the tool was removed and uninstalled; otherwise, false.</returns>
    Task<bool> RemoveToolAsync(string packageName, bool global);

    /// <summary>
    /// Lists all scaffold tools currently in the manifest.
    /// </summary>
    void ListTools();
}
