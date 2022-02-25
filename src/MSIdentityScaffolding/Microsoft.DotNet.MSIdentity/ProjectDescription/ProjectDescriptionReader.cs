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
        private const string ProjectTypeIdSuffix = "dotnet-";

        private readonly IEnumerable<string> _files;
        public List<ProjectDescription> ProjectDescriptions { get; }

        public ProjectDescriptionReader(IEnumerable<string> files)
        {
            _files = files;
            ProjectDescriptions = InitializeProjectDescriptions();
        }

        public ProjectDescription? GetProjectDescription(string projectTypeIdentifier)
        {
            string? projectTypeId = projectTypeIdentifier;
            if (string.IsNullOrEmpty(projectTypeId) || projectTypeId == ProjectTypeIdSuffix)
            {
                projectTypeId = InferProjectType();
            }

            return projectTypeId != null ? ReadProjectDescription(projectTypeId) : null;
        }

        private ProjectDescription? ReadProjectDescription(string identifier) => ProjectDescriptions.FirstOrDefault(p => p.Identifier == identifier);

        static readonly JsonSerializerOptions serializerOptionsWithComments = new JsonSerializerOptions
        {
            ReadCommentHandling = JsonCommentHandling.Skip
        };

        private ProjectDescription? ReadDescriptionFromFileContent(byte[] fileContent)
        {
            string jsonText = Encoding.UTF8.GetString(fileContent);
            return JsonSerializer.Deserialize<ProjectDescription>(jsonText, serializerOptionsWithComments);
        }

        private string? InferProjectType()
        {
            // TODO: could be both a Web app and WEB API.
            foreach (ProjectDescription projectDescription in ProjectDescriptions)
            {
                var mergedMatches = projectDescription.GetMergedMatchesForProjectType(ProjectDescriptions);
                var foundMatches = mergedMatches.Where(matches => HasMatch(matches));
                if (foundMatches.Any())
                {
                    return projectDescription.Identifier;
                }
            }

            return null;
        }

        /// <summary>
        /// Checks for a match given a list of matches and a list of files
        /// </summary>
        /// <param name="matches"></param>
        /// <returns></returns>
        private bool HasMatch(MatchesForProjectType matches)
        {
            if (string.IsNullOrEmpty(matches.FileRelativePath))
            {
                return HasMatchingDirectory(matches.FolderRelativePath, matches.FileExtension, matches.MatchAny);
            }

            return HasFileWithMatch(matches.FileRelativePath, matches.FolderRelativePath, matches.MatchAny);
        }

        private bool HasMatchingDirectory(string? folderRelativePath, string? fileExtension, string[]? matchAny)
        {
            if (string.IsNullOrEmpty(folderRelativePath))
            {
                return false;
            }

            var matchingPaths = _files.Where(file => DirectoryMatches(file, folderRelativePath, fileExtension));

            return AnyFileContainsMatch(matchAny, matchingPaths);
        }

        private bool HasFileWithMatch(string fileRelativePath, string? folderRelativePath, string[]? matchAny)
        {
            var matchingFilePaths = _files.Where(filePath => Path.GetFileName(filePath).Equals(fileRelativePath, StringComparison.OrdinalIgnoreCase));

            if (!string.IsNullOrEmpty(folderRelativePath))
            {
                matchingFilePaths = matchingFilePaths.Where(filePath => DirectoryMatches(filePath, folderRelativePath));
            }

            return AnyFileContainsMatch(matchAny, matchingFilePaths);
        }

        private static bool DirectoryMatches(string filePath, string folderRelativePath, string? fileExtension = null)
        {
            var directoryPath = Path.GetDirectoryName(filePath);
            var folderFound = directoryPath?.Contains(folderRelativePath, StringComparison.OrdinalIgnoreCase) ?? false;
            var extensionMatches = fileExtension?.Equals(Path.GetExtension(filePath)) ?? true; // If extension is null, no need to match

            return folderFound && extensionMatches;
        }

        private static bool AnyFileContainsMatch(string[]? matchAny, IEnumerable<string> matchingPaths)
        {
            if (matchAny is null)
            {
                return matchingPaths.Any();
            }

            var matchingFiles = matchingPaths.Where(filePath => FileMatches(filePath, matchAny));
            return matchingFiles.Any(); // If MatchAny is not null, at least file needs to contain a match
        }

        private static bool FileMatches(string filePath, string[] matchAny)
        {
            try
            {
                var fileContent = File.ReadAllText(filePath);
                var fileMatches = matchAny.Where(match => fileContent.Contains(match, StringComparison.OrdinalIgnoreCase));
                return fileMatches.Any();
            }
            catch
            {
                return false;
            }
        }

        public List<ProjectDescription> InitializeProjectDescriptions()
        {
            var projectDescriptions = new List<ProjectDescription>();
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

            return projectDescriptions;
            //  TODO: provide an extension mechanism to add such files outside the tool.
            //     In that case the validation would not be an exception? but would need to provide error messages
        }
    }
}
