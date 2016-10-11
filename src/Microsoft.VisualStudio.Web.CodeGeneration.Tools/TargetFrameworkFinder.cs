// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml;
using Microsoft.Build.Construction;
using Microsoft.Build.Evaluation;
using NuGet.Frameworks;

namespace Microsoft.VisualStudio.Web.CodeGeneration.Tools
{
    internal class TargetFrameworkFinder
    {
        internal static IEnumerable<NuGetFramework> GetAvailableTargetFrameworks(string projectFilePath)
        {
            var project = CreateProject(projectFilePath);
            return GetAvailableTargetFrameworks(project);
        }

        private static Project CreateProject(string projectFilePath)
        {
            using (var stream = new FileStream(projectFilePath, FileMode.Open, FileAccess.Read))
            {
                var xmlReader = XmlReader.Create(stream);

                var projectCollection = new ProjectCollection();
                var xml = ProjectRootElement.Create(xmlReader, projectCollection, preserveFormatting: true);
                xml.FullPath = projectFilePath;

                return new Project(xml, globalProperties: null, toolsVersion: null, projectCollection: projectCollection);
            }
        }

        private static IEnumerable<NuGetFramework> GetAvailableTargetFrameworks(Project project)
        {
            if (project == null)
            {
                throw new ArgumentNullException(nameof(project));
            }

            var frameworkProperty = project.GetProperty("TargetFrameworks")?.EvaluatedValue;
            if (string.IsNullOrEmpty(frameworkProperty))
            {
                return null;
            }
            var tfms = frameworkProperty.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries);

            return tfms.Select(tfm => NuGetFramework.Parse(tfm));
        }
    }
}