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

        public static NuGetFramework GetSuitableFrameworkFromProject(System.Collections.Generic.IEnumerable<NuGetFramework> frameworksInProject)
        {
            var nearestFramework = NuGetFrameworkUtility.GetNearest(
                                frameworksInProject,
                                 FrameworkConstants.CommonFrameworks.NetCoreApp10,
                                 f => new NuGetFramework(f));

            if (nearestFramework == null)
            {
                nearestFramework = NuGetFrameworkUtility.GetNearest(
                    frameworksInProject,
                FrameworkConstants.CommonFrameworks.Net46,
                f => new NuGetFramework(f));
            }
            if (nearestFramework == null)
            {
                // This should never happen as long as we dispatch correctly.
                var msg = Resources.NoCompatibleFrameworks
                    + Environment.NewLine
                    + string.Format(Resources.AvailableFrameworks, string.Join($"{Environment.NewLine} -", frameworksInProject.Select(f => f.GetShortFolderName())));
                throw new InvalidOperationException(msg);
            }

            return nearestFramework;
        }
    }
}