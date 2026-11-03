// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using Microsoft.CodeAnalysis;

namespace Microsoft.DotNet.Scaffolding.Roslyn.Services;

public interface ICodeService
{
    /// <summary>
    /// Returns current Roslyn workspace object.
    /// </summary>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    Task<Workspace?> GetWorkspaceAsync();

    /// <summary>
    /// Attempts to apply changes to current Roslyn workspace and returns true if it was successful.
    /// </summary>
    /// <param name="solution"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    bool TryApplyChanges(Solution? solution);

    /// <summary>
    /// Reloads Roslyn workspace.
    /// </summary>
    /// <param name="projectPath"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    Task ReloadWorkspaceAsync();

    Task <List<ISymbol>> GetAllClassSymbolsAsync();

    /// <summary>
    /// Gets class symbols from the current project and, optionally, its transitive project references.
    /// Classes from the current project are returned first. Package references are not searched.
    /// </summary>
    /// <remarks>
    /// Implementations predating this overload retain current-project-only discovery.
    /// They must override this method to support referenced projects.
    /// </remarks>
    /// <param name="includeReferencedProjects">Whether to include classes declared in referenced projects.</param>
    /// <exception cref="NotSupportedException">The implementation does not support referenced-project discovery.</exception>
    Task<List<ISymbol>> GetAllClassSymbolsAsync(bool includeReferencedProjects)
        => includeReferencedProjects
            ? throw new NotSupportedException("This code service does not support referenced-project class discovery.")
            : GetAllClassSymbolsAsync();

    Task<List<Document>> GetAllDocumentsAsync();
    Task<Document?> GetDocumentAsync(string? documentName);
}
