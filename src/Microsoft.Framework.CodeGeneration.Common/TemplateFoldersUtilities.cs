// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.Framework.Runtime;

namespace Microsoft.Framework.CodeGeneration
{
    internal static class TemplateFoldersUtilities
    {
        public static IEnumerable<string> GetTemplateFolders(
            [NotNull]string containingProject,
            [NotNull]ILibraryManager libraryManager,
            [NotNull]IApplicationEnvironment appEnvironment)
        {
            string templatesFolderName = "Templates";
            var templateFolders = new List<string>();

            var projReference = GetProjectReference(containingProject, libraryManager, appEnvironment);
            if (projReference != null)
            {
                templateFolders.Add(Path.Combine(
                    Path.GetDirectoryName(projReference.ProjectPath),
                    templatesFolderName));
            }
            //Todo: Get the path of  executing assembly and add it to template folders
            //var webFxAssemblyLocation = typeof(ViewGenerator).GetTypeInfo().Assembly.CodeBase;
            //if (!string.IsNullOrEmpty(webFxAssemblyLocation))
            //{
            //    templateFolders.Add(Path.Combine(webFxAssemblyLocation, templatesFolderName));
            //}
            return templateFolders;
        }

        private static IMetadataProjectReference GetProjectReference(string projectReferenceName,
            ILibraryManager libraryManager, IApplicationEnvironment appEnvironment)
        {
            return libraryManager
                .GetLibraryExport(projectReferenceName)
                .MetadataReferences
                .OfType<IRoslynMetadataReference>()
                .OfType<IMetadataProjectReference>()
                .FirstOrDefault();
        }
    }
}