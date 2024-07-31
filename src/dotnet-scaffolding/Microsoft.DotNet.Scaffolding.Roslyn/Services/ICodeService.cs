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
    Task<List<Document>> GetAllDocumentsAsync();
    Task<Document?> GetDocumentAsync(string? documentName);
}
