// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.Web.CodeGeneration.ProjectInfo;

namespace Microsoft.VisualStudio.Web.CodeGeneration.MsBuild
{
    public class MsBuildProjectFileReader
    {
        public static MsBuildProjectFile ReadProjectFile(string path, Dictionary<string, string> globalProperties)
        {
            Requires.NotNullOrEmpty(path, nameof(path));
            Requires.NotNull(globalProperties, nameof(globalProperties));

            var project = ProjectUtilities.CreateProject(path, globalProperties);

            return CreateProjectFileFromProject(project);
        }

        public static MsBuildProjectFile CreateProjectFileFromProject(Build.Evaluation.Project project)
        {
            Requires.NotNull(project, nameof(project));

            return new MsBuildProjectFile(project.FullPath,
                project.GetItems("Compile").Select(i => i.EvaluatedInclude),
                project.GetItems("ProjectReference").Select(i => i.EvaluatedInclude),
                project.GetItems("Reference").Select(i => i.EvaluatedInclude),
                project.GlobalProperties,
                project.GetPropertyValue("TargetFrameworks"));
        }

        public static MsBuildProjectFile ReadProjectFile(string path)
        {
            return ReadProjectFile(path, new Dictionary<string, string>());
        }
    }
}
