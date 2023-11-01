// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.DotNet.Scaffolding.Shared.Project;

namespace Microsoft.DotNet.Scaffolding.Shared.CodeModifier
{
    internal static class ProjectExtensions
    {
        public static CodeAnalysis.Project WithAllSourceFiles(this CodeAnalysis.Project project, IEnumerable<string> files)
        {
            foreach (string file in files)
            {
                project = project.AddDocument(file, File.ReadAllText(file)).Project;
            }

            return project;
        }

        //Given CodeAnalysis.Project and ModelType, return CodeAnalysis.Document by reading the latest file from disk.
        //Need CodeAnalysis.Project for AddDocument method.
        public static Document GetUpdatedDocument(this CodeAnalysis.Project project, IFileSystem fileSystem, ModelType type)
        {
            if (project != null && type != null)
            {
                string filePath = type.TypeSymbol?.Locations.FirstOrDefault()?.SourceTree?.FilePath;
                string fileText = fileSystem.ReadAllText(filePath);
                if (!string.IsNullOrEmpty(fileText))
                {
                    return project.AddDocument(filePath, fileText);
                }
            }

            return null;
        }
    }
}
