// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;

namespace Microsoft.DotNet.MSIdentity.Project
{
    public class ProjectDescription
    {
        /// <summary>
        /// Empty files
        /// </summary>
        static readonly ConfigurationProperties[] s_emptyFiles = Array.Empty<ConfigurationProperties>();

        /// <summary>
        /// Identifier of the project description.
        /// For instance dotnet-webapi
        /// </summary>
        public string? Identifier { get; set; }

        public string? ProjectRelativeFolder { get; set; }

        public string? BasedOnProjectDescription { get; set; }

        public string[]? BasePackages { get; set; }
        public string[]? CommonPackages { get; set; }
        public string[]? DownstreamApiPackages { get; set; }
        public string[]? MicrosoftGraphPackages { get; set; }
        public override string? ToString()
        {
            return Identifier;
        }

        /// <summary>
        /// Is the project description valid?
        /// </summary>
        /// <returns></returns>
        public bool IsValid()
        {
            bool isValid = !string.IsNullOrEmpty(Identifier)
                && ProjectRelativeFolder != null
                && (ConfigurationProperties != null || MatchesForProjectType != null)
                && (ConfigurationProperties == null || !ConfigurationProperties.Any(c => !c.IsValid()))
                && (MatchesForProjectType == null || !MatchesForProjectType.Any(m => !m.IsValid()));

            return isValid;
        }

        public ConfigurationProperties[]? ConfigurationProperties { get; set; }

        public MatchesForProjectType[]? MatchesForProjectType { get; set; }

        public ProjectDescription? GetBasedOnProject(IEnumerable<ProjectDescription> projects)
        {
            if (string.IsNullOrEmpty(BasedOnProjectDescription))
            {
                return null;
            }

            if (projects is null)
            {
                throw new ArgumentNullException(nameof(projects));
            }

            ProjectDescription? baseProject = projects.FirstOrDefault(p => p.Identifier == BasedOnProjectDescription);
            if (baseProject == null)
            {
                throw new FormatException($"BasedOnProjectDescription = {BasedOnProjectDescription} could not be found in project {ProjectRelativeFolder} . ");
            }

            return baseProject;
        }

        /// <summary>
        /// Get all the files with including merged from BaseOn project recursively
        /// merging all the properties
        /// </summary>
        /// <param name="projects"></param>
        /// <returns></returns>
        public IEnumerable<ConfigurationProperties> GetMergedConfigurationProperties(IEnumerable<ProjectDescription> projects)
        {
            IEnumerable<ConfigurationProperties> configurationProperties = GetBasedOnProject(projects)?.GetMergedConfigurationProperties(projects) ?? s_emptyFiles;
            IEnumerable<ConfigurationProperties> allConfigurationProperties = ConfigurationProperties != null ? configurationProperties.Union(ConfigurationProperties) : configurationProperties;
            var allConfigurationPropertiesGrouped = allConfigurationProperties.GroupBy(f => f.FileRelativePath);
            foreach (var fileGrouping in allConfigurationPropertiesGrouped)
            {
                yield return new ConfigurationProperties
                {
                    FileRelativePath = fileGrouping.Key,
                    Properties = fileGrouping.SelectMany(f => f.Properties).ToArray(),
                };
            }
        }

        /// <summary>
        /// Get all the files with including merged from BaseOn project recursively
        /// merging all the properties
        /// </summary>
        /// <param name="projects"></param>
        /// <returns></returns>
        public IEnumerable<MatchesForProjectType> GetMergedMatchesForProjectType(IEnumerable<ProjectDescription> projects)
        {
            var configurationProperties = GetBasedOnProject(projects)?.GetMergedMatchesForProjectType(projects) ?? new MatchesForProjectType[0];
            return MatchesForProjectType?.Union(configurationProperties) ?? configurationProperties;
        }
    }
}
