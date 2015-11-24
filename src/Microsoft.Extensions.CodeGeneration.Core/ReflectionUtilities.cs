// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.Extensions.CompilationAbstractions;
using Microsoft.Extensions.PlatformAbstractions;

namespace Microsoft.Extensions.CodeGeneration
{
    public static class ReflectionUtilities
    {
        public static Type GetReflectionType(
            this ILibraryExporter libraryExporter,
            ILibraryManager libraryManager,
            IApplicationEnvironment environment,
            string name)
        {
            if (libraryExporter == null)
            {
                throw new ArgumentNullException(nameof(libraryExporter));
            }

            if (libraryManager == null)
            {
                throw new ArgumentNullException(nameof(libraryManager));
            }

            if (environment == null)
            {
                throw new ArgumentNullException(nameof(environment));
            }

            return libraryExporter
                .GetProjectAssemblies(libraryManager, environment)
                .Select(asm => asm.GetType(name))
                .Where(type => type != null)
                .FirstOrDefault();
        }

        public static IEnumerable<Assembly> GetProjectAssemblies(
            this ILibraryExporter libraryExporter,
            ILibraryManager libraryManager,
            IApplicationEnvironment environment)
        {
            if (libraryExporter == null)
            {
                throw new ArgumentNullException(nameof(libraryExporter));
            }

            if (libraryManager == null)
            {
                throw new ArgumentNullException(nameof(libraryManager));
            }

            if (environment == null)
            {
                throw new ArgumentNullException(nameof(environment));
            }

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