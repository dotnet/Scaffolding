// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Xml;
using Microsoft.Build.Construction;
using Microsoft.Build.Evaluation;
using Microsoft.Build.Execution;
using Microsoft.VisualStudio.Web.CodeGeneration.ProjectInfo;
using Microsoft.Build.Framework;

namespace Microsoft.VisualStudio.Web.CodeGeneration.MsBuild
{
    public class ProjectUtilities
    {
        private const string ItemSpecKey = "OriginalItemSpec";
        private const string NugetSourceTypeKey = "NugetSourceType";
        private const string VersionKey = "Version";

        public static Microsoft.Build.Evaluation.Project CreateProject(string filePath, IDictionary<string, string> globalProperties)
        {
            Microsoft.Build.Evaluation.Project project = null;
            using (var fileStream = new FileStream(filePath, FileMode.Open))
            {
                var xmlReader = XmlReader.Create(fileStream);
                var projectCollection = new ProjectCollection();
                var xml = ProjectRootElement.Create(xmlReader, projectCollection, preserveFormatting: true);
                xml.FullPath = filePath;

                project = new Microsoft.Build.Evaluation.Project(xml, globalProperties, /*toolsVersion*/ null, projectCollection);
            }

            return project;
        }

        public static ResolvedReference CreateResolvedReferenceFromProjectItem(ProjectItemInstance item)
        {
            string itemSpec = string.Empty;
            string nugetPackageSource = string.Empty;
            string version = string.Empty;
            string resolvedPath = string.Empty;
            string name = string.Empty;

            resolvedPath = item.EvaluatedInclude;

            name = resolvedPath ?? Path.GetFileNameWithoutExtension(resolvedPath);

            foreach (var m in item.Metadata)
            {
                if (ItemSpecKey.Equals(m.Name, StringComparison.OrdinalIgnoreCase))
                {
                    itemSpec = m.EvaluatedValue;
                    continue;
                }

                if (NugetSourceTypeKey.Equals(m.Name, StringComparison.OrdinalIgnoreCase))
                {
                    nugetPackageSource = m.EvaluatedValue;
                    continue;
                }

                if (VersionKey.Equals(m.Name, StringComparison.OrdinalIgnoreCase))
                {
                    version = m.EvaluatedValue;
                }
            }

            return new ResolvedReference(name, itemSpec, nugetPackageSource, resolvedPath, version);
        }

        public static DependencyDescription CreateDependencyDescriptionFromTaskItem(ITaskItem item)
        {
            Requires.NotNull(item, nameof(item));


            var version = item.GetMetadata("Version");
            var path = item.GetMetadata("Path");
            var type = item.GetMetadata("Type");
            var resolved = item.GetMetadata("Resolved");
            var itemSpec = item.ItemSpec;

            // For type == Target, we do not get Name in the metadata. This is a special node where the dependencies are 
            // the direct dependencies of the project.
            var name = ("Target".Equals(type, StringComparison.OrdinalIgnoreCase))
                ? itemSpec
                : item.GetMetadata("Name");

            if (string.IsNullOrEmpty(name))
            {
                return null;
            }

            return new DependencyDescription(name, path, itemSpec, version, type, resolved);
        }
    }
}
