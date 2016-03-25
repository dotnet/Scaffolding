// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Reflection;
using Microsoft.DotNet.ProjectModel.Compilation;
using Microsoft.DotNet.ProjectModel;
using Microsoft.DotNet.ProjectModel.Graph;



namespace Microsoft.Extensions.CodeGeneration.DotNet
{
    public static class LibraryExporterExtensions
    {
        public static string GetResolvedPathForDependency(this ILibraryExporter _libraryExporter, LibraryDescription library)
        {
            if (library == null)
            {
                throw new ArgumentNullException(nameof(library));
            }

            var exports = _libraryExporter.GetAllExports();
            var assets = exports
                .SelectMany(export => export.RuntimeAssemblyGroups.GetDefaultGroup().Assets)
                .Where(asset => asset.Name == library.Identity.Name);

            if (assets.Any())
            {
                return assets.First().ResolvedPath;
            }

            assets = exports
                .SelectMany(export => export.NativeLibraryGroups.GetDefaultGroup().Assets)
                .Where(asset => asset.Name == library.Identity.Name);
            if (assets.Any())
            {
                return assets.First().ResolvedPath;
            }

            assets = exports
                .SelectMany(export => export.CompilationAssemblies)
                .Where(asset => asset.Name == library.Identity.Name);
            if (assets.Any())
            {
                return assets.First().ResolvedPath;
            }

            return string.Empty;
        }
    }
}