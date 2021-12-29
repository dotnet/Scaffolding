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
        public List<ProjectDescription> projectDescriptions { get; private set; } = new List<ProjectDescription>();

        public ProjectDescription? GetProjectDescription(string projectTypeIdentifier, string projectPath)
        {
            string? projectTypeId = projectTypeIdentifier;
            if (string.IsNullOrEmpty(projectTypeId) || projectTypeId == "dotnet-")
            {
                projectTypeId = InferProjectType(projectPath);
            }

            return projectTypeId != null ? ReadProjectDescription(projectTypeId) : null;
        }

        private ProjectDescription? ReadProjectDescription(string projectTypeIdentifier)
        {
            ReadProjectDescriptions();

            return projectDescriptions.FirstOrDefault(projectDescription => projectDescription.Identifier == projectTypeIdentifier);
        }

        static JsonSerializerOptions serializerOptionsWithComments = new JsonSerializerOptions()
        {
            ReadCommentHandling = JsonCommentHandling.Skip
        };

        private ProjectDescription? ReadDescriptionFromFileContent(byte[] fileContent)
        {
            string jsonText = Encoding.UTF8.GetString(fileContent);
            return JsonSerializer.Deserialize<ProjectDescription>(jsonText, serializerOptionsWithComments);
        }

        private string? InferProjectType(string projectPath)
        {
            if (!Directory.Exists(projectPath))
            {
                return null;
            }

            ReadProjectDescriptions();

            // TODO: could be both a Web app and WEB API.
            foreach (ProjectDescription projectDescription in projectDescriptions)
            {
                var matchesForProjectTypes = projectDescription.GetMergedMatchesForProjectType(projectDescriptions);
                if (!matchesForProjectTypes.Any())
                {
                    return null;
                }

                foreach (MatchesForProjectType matchesForProjectType in matchesForProjectTypes)
                {
                    if (!string.IsNullOrEmpty(matchesForProjectType.FileRelativePath))
                    {
                        try
                        {
                            IEnumerable<string> files = Directory.EnumerateFiles(projectPath, matchesForProjectType.FileRelativePath);
                            if (files.Any())
                            {
                                if (matchesForProjectType.MatchAny is null)
                                {
                                    return projectDescription.Identifier; // If MatchAny is null, then the presence of a file is enough
                                }
                                foreach (string filePath in files)
                                {
                                    string fileContent = File.ReadAllText(filePath) ?? string.Empty;
                                    var hasMatch = matchesForProjectType.MatchAny.Where(match => fileContent.Contains(match)).Any();
                                    if (hasMatch)
                                    {
                                        return projectDescription.Identifier!;  // If there are matches, at least one needs to match
                                    }
                                }
                            }
                        }
                        catch
                        {
                            // files not found
                        }
                    }

                    if (!string.IsNullOrEmpty(matchesForProjectType.FolderRelativePath))
                    {
                        try
                        {
                            if (Directory.EnumerateDirectories(projectPath, matchesForProjectType.FolderRelativePath).Any())
                            {
                                return projectDescription.Identifier!;
                            }
                        }
                        catch
                        {
                            continue; // Folder not found
                        }
                    }
                }
            }

            return null;
        }

        private void ReadProjectDescriptions()
        {
            if (projectDescriptions.Any())
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
                    projectDescriptions.Add(projectDescription);
                }
            }

            // TODO: provide an extension mechanism to add such files outside the tool.
            // In that case the validation would not be an exception? but would need to provide error messages
        }
    }
}
