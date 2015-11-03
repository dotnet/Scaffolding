// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.Extensions.Compilation;
using Microsoft.Extensions.PlatformAbstractions;

namespace Microsoft.Extensions.CodeGeneration
{
    public static class ReflectionUtilities
    {
        public static Type GetReflectionType(
            [NotNull]this ILibraryExporter libraryExporter,
            [NotNull]ILibraryManager libraryManager,
            [NotNull]IApplicationEnvironment environment,
            string name)
        {
            return libraryExporter
                .GetProjectAssemblies(libraryManager, environment)
                .Select(asm => asm.GetType(name))
                .Where(type => type != null)
                .FirstOrDefault();
        }

        public static IEnumerable<Assembly> GetProjectAssemblies(
            [NotNull]this ILibraryExporter libraryExporter,
            [NotNull]ILibraryManager libraryManager,
            [NotNull]IApplicationEnvironment environment)
        {
            return libraryExporter
                .GetProjectsInApp(environment)
                .Select(comp => libraryManager.GetLibrary(comp.Compilation.AssemblyName))
                .Select(lib => LoadAssembly(lib.Name));
        }

        private static Assembly LoadAssembly(string name)
        {
            return Assembly.Load(new AssemblyName(name));
        }
    }
}