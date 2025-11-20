// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
namespace Microsoft.DotNet.Scaffolding.TextTemplating;

/// <summary>
/// Service interface for locating template folders and files for T4 text templating.
/// </summary>
internal interface ITemplateFolderService
{
    /// <summary>
    /// Gets the template folders for the specified base folders.
    /// </summary>
    /// <param name="baseFolders">The base folder names to search under.</param>
    /// <returns>Enumerable of template folder paths.</returns>
    IEnumerable<string> GetTemplateFolders(string[] baseFolders);
    /// <summary>
    /// Gets all T4 template files (.tt) under the specified base folders.
    /// </summary>
    /// <param name="baseFolders">The base folder names to search under.</param>
    /// <returns>Enumerable of T4 template file paths.</returns>
    IEnumerable<string> GetAllT4Templates(string[] baseFolders);
    /// <summary>
    /// Gets all files with the specified extension under the base folders.
    /// </summary>
    /// <param name="baseFolders">The base folder names to search under.</param>
    /// <param name="extension">The file extension to search for (e.g., ".tt").</param>
    /// <returns>Enumerable of file paths.</returns>
    IEnumerable<string> GetAllFiles(string[] baseFolders, string extension);
}
