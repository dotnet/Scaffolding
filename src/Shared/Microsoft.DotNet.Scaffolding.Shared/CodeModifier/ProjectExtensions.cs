// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
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

        /// <summary>
        /// Given CodeAnalysis.Project and fileName, return CodeAnalysis.Document by reading the latest file from disk.
        /// </summary>
        /// <param name="project"></param>
        /// <param name="fileName"></param>
        /// <param name="fileSystem"></param>
        /// <returns></returns>
        public static Document GetDocumentFromName(this CodeAnalysis.Project project, string fileName, IFileSystem fileSystem)
        {
            if (project == null || string.IsNullOrEmpty(fileName))
            {
                return null;
            }

            foreach (var candidateProject in EnumerateCandidateProjects(project))
            {
                var projectDirectory = Path.GetDirectoryName(candidateProject.FilePath);
                if (string.IsNullOrEmpty(projectDirectory))
                {
                    continue;
                }

                string extension = Path.GetExtension(fileName);
                if (!string.IsNullOrEmpty(extension))
                {
                    extension = $"*{extension}";
                }

                var allFiles = Directory.EnumerateFiles(projectDirectory, extension, SearchOption.AllDirectories);
                string filePath = allFiles.FirstOrDefault(x => x.EndsWith(fileName, StringComparison.OrdinalIgnoreCase));
                if (!string.IsNullOrEmpty(filePath))
                {
                    string fileText = fileSystem.ReadAllText(filePath);
                    if (!string.IsNullOrEmpty(fileText))
                    {
                        return candidateProject.AddDocument(filePath, fileText);
                    }
                }
            }

            return null;
        }

        private static IEnumerable<CodeAnalysis.Project> EnumerateCandidateProjects(CodeAnalysis.Project project)
        {
            var queue = new Queue<CodeAnalysis.Project>();
            var visited = new HashSet<string>();
            queue.Enqueue(project);

            while (queue.Count > 0)
            {
                var current = queue.Dequeue();
                if (current == null)
                {
                    continue;
                }

                var key = current.FilePath ?? current.Name;
                if (!visited.Add(key))
                {
                    continue;
                }

                yield return current;

                foreach (var projectReference in current.ProjectReferences)
                {
                    var referencedProject = current.Solution.GetProject(projectReference.ProjectId);
                    if (referencedProject != null)
                    {
                        queue.Enqueue(referencedProject);
                    }
                }
            }
        }
    }
}
