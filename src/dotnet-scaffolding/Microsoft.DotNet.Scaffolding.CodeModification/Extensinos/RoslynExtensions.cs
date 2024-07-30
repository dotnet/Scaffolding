// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using Microsoft.CodeAnalysis;

namespace Microsoft.DotNet.Scaffolding.CodeModification.Extensions;

public static class RoslynExtensions
{
    public static Project? GetProject(this Solution? solution, string? projectPath)
    {
        return solution?.Projects?.FirstOrDefault(x => string.Equals(projectPath, x.FilePath, StringComparison.OrdinalIgnoreCase));
    }

    public static Document? GetDocument(this Project project, string? documentName)
    {
        var fileName = Path.GetFileName(documentName);
        if (string.IsNullOrEmpty(fileName))
        {
            return null;
        }

        //often Document.Name is the file path of the document and not the name.
        //check for all possible cases. 
        return project.Documents.FirstOrDefault(x =>
            x.Name.EndsWith(fileName, StringComparison.OrdinalIgnoreCase) ||
            x.Name.Equals(documentName, StringComparison.OrdinalIgnoreCase) ||
            (!string.IsNullOrEmpty(x.FilePath) &&
            x.FilePath.Equals(documentName, StringComparison.OrdinalIgnoreCase)));
    }
}
