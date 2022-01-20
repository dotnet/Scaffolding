// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.Json;

namespace Microsoft.DotNet.MSIdentity.Project
{
    public class ProjectDescriptionReader
    {
        private IEnumerable<string>? _directories;
        internal IEnumerable<string> Directories => _directories ??= Directory.EnumerateDirectories(ProjectPath);

        public List<ProjectDescription> ProjectDescriptions { get; private set; } = new List<ProjectDescription>();

        private string ProjectPath { get; }

        public ProjectDescriptionReader(string projectPath)
        {
            ProjectPath = projectPath;
        }

        public ProjectDescription? GetProjectDescription(string projectTypeIdentifier, IEnumerable<string> files)
        {
            string? projectTypeId = projectTypeIdentifier;
            if (string.IsNullOrEmpty(projectTypeId) || projectTypeId == "dotnet-")
            {
                projectTypeId = InferProjectType(files);
            }

            return projectTypeId != null ? ReadProjectDescription(projectTypeId) : null;
        }

        private ProjectDescription? ReadProjectDescription(string projectTypeIdentifier)
        {
            ReadProjectDescriptions();

            return ProjectDescriptions.FirstOrDefault(projectDescription => projectDescription.Identifier == projectTypeIdentifier);
        }

        static readonly JsonSerializerOptions serializerOptionsWithComments = new JsonSerializerOptions()
        {
            ReadCommentHandling = JsonCommentHandling.Skip
        };

        private ProjectDescription? ReadDescriptionFromFileContent(byte[] fileContent)
        {
            string jsonText = Encoding.UTF8.GetString(fileContent);
            return JsonSerializer.Deserialize<ProjectDescription>(jsonText, serializerOptionsWithComments);
        }

        private string? InferProjectType(IEnumerable<string> files)
        {
            ReadProjectDescriptions();

            // TODO: could be both a Web app and WEB API.
            foreach (ProjectDescription projectDescription in ProjectDescriptions)
            {
                var mergedMatches = projectDescription.GetMergedMatchesForProjectType(ProjectDescriptions);
                foreach (MatchesForProjectType matches in mergedMatches)
                {
                    if (HasMatch(matches, files))
                    {
                        return projectDescription.Identifier;
                    }
                   
                }
            }

            return null;
        }

        /// <summary>
        /// TODO
        /// </summary>
        /// <param name="matchesForProjectType"></param>
        /// <param name="projectPath"></param>
        /// <returns></returns>
        private bool HasMatch(MatchesForProjectType matchesForProjectType, IEnumerable<string> filePaths)
        {
            if (!string.IsNullOrEmpty(matchesForProjectType.FileRelativePath))
            {
                IEnumerable<string>? matchingFiles = filePaths.Where(file => file.Contains(matchesForProjectType.FileRelativePath));
                foreach (string filePath in matchingFiles)
                {
                    if (matchesForProjectType.MatchAny is null)
                    {
                        return true; // If MatchAny is null, then the presence of a file is enough
                    }

                    var fileContent = File.ReadAllText(filePath);
                    var hasMatch = matchesForProjectType.MatchAny.Where(match => fileContent.Contains(match)).Any();
                    if (hasMatch)
                    {
                        return true;  // If MatchAny is not null, at least one needs to match
                    }
                }
            }

            if (!string.IsNullOrEmpty(matchesForProjectType.FolderRelativePath))
            {
                return Directories.Where(dir => dir.Contains(matchesForProjectType.FolderRelativePath)).Any();
            }

            return false;
        }

        private void ReadProjectDescriptions()
        {
            if (ProjectDescriptions.Any())
            {
                return;
            }

            foreach (PropertyInfo propertyInfo in AppProvisioningTool.Properties)
            {
                if (!(propertyInfo.Name.StartsWith("cm") || propertyInfo.Name.StartsWith("add")))
                {
                    byte[] content = (propertyInfo.GetValue(null) as byte[])!;
                    ProjectDescription? projectDescription = ReadDescriptionFromFileContent(content);

                    if (projectDescription == null)
                    {
                        throw new FormatException($"Resource file { propertyInfo.Name } could not be parsed. ");
                    }
                    if (!projectDescription.IsValid())
                    {
                        throw new FormatException($"Resource file {propertyInfo.Name} is missing Identitier or ProjectRelativeFolder is null. ");
                    }

                    ProjectDescriptions.Add(projectDescription);
                }
            }

            // TODO: provide an extension mechanism to add such files outside the tool.
            // In that case the validation would not be an exception? but would need to provide error messages
        }
    }
}
