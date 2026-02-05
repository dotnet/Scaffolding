// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
namespace Microsoft.DotNet.Scaffolding.TextTemplating;

/// <summary>
/// Service interface for locating template folders and files for T4 text templating.
/// </summary>
internal interface ITemplateFolderService
{
    /// <summary>
    /// Gets all T4 template files (.tt) under the specified base folders for the project's target framework.
    /// </summary>
    /// <param name="baseFolders">The base folder names to search under.</param>
    /// <param name="projectPath">The path to the project.</param>
    /// <returns>Enumerable of T4 template file paths.</returns>
    IEnumerable<string> GetAllT4TemplatesForTargetFramework(string[] baseFolders, string? projectPath);
}
