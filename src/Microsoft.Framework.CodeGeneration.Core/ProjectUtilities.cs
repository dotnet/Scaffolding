// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.Framework.Runtime;

namespace Microsoft.Framework.CodeGeneration
{
    public static class ProjectUtilities
    {
        private static string[] _frameworkProjectNames = new[]
        {
            "Microsoft.Framework.CodeGeneration",
            "Microsoft.Framework.CodeGeneration.Core",
            "Microsoft.Framework.CodeGeneration.Templating",
            "Microsoft.Framework.CodeGeneration.Common",
            "Microsoft.Framework.CodeGenerators.WebFx",
        };

        public static CompilationReference GetProject(
            [NotNull]this ILibraryManager libraryManager,
            [NotNull]IApplicationEnvironment environment)
        {
            var export = libraryManager.GetLibraryExport(environment.ApplicationName);

            var project = export.MetadataReferences
                .OfType<IMetadataProjectReference>()
                .OfType<IRoslynMetadataReference>()
                .Select(reference => reference.MetadataReference as CompilationReference)
                .Where(compilation => string.Equals(environment.ApplicationName, compilation.Compilation.AssemblyName))
                .FirstOrDefault();

            Contract.Assert(project != null);
            return project;
        }

        public static IEnumerable<CompilationReference> GetProjectsInApp(
            [NotNull]this ILibraryManager libraryManager,
            [NotNull]IApplicationEnvironment environment)
        {
            var export = libraryManager.GetLibraryExport(environment.ApplicationName);

            return export.MetadataReferences
                .OfType<IMetadataProjectReference>()
                .OfType<IRoslynMetadataReference>()
                .Select(reference => reference.MetadataReference as CompilationReference)
                .Where(compilation => !_frameworkProjectNames.Contains(compilation.Compilation.AssemblyName));
        }
    }
}