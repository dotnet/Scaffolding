// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.Framework.Runtime;

namespace Microsoft.Framework.CodeGeneration
{
    public static class ReflectionUtilities
    {
        public static Type GetReflectionType(
            [NotNull]this ILibraryManager libraryManager,
            [NotNull]IApplicationEnvironment environment,
            string name)
        {
            return libraryManager
                .GetProjectAssemblies(environment)
                .Select(asm => asm.GetType(name))
                .Where(type => type != null)
                .FirstOrDefault();
        }

        public static IEnumerable<Assembly> GetProjectAssemblies(
            [NotNull]this ILibraryManager libraryManager,
            [NotNull]IApplicationEnvironment environment)
        {
            return libraryManager
                .GetProjectsInApp(environment)
                .Select(comp => libraryManager.GetLibraryInformation(comp.Compilation.AssemblyName))
                .Select(lib => LoadAssembly(lib.Name));
        }

        private static Assembly LoadAssembly(string name)
        {
            return Assembly.Load(new AssemblyName(name));
        }
    }
}