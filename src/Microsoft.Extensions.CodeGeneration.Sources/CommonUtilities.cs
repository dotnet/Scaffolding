// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.IO;
using System.Linq;
using System.Reflection;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Emit;
using Microsoft.Extensions.PlatformAbstractions;

namespace Microsoft.Extensions.CodeGeneration
{
    internal static class CommonUtilities
    {
        public static CompilationResult GetAssemblyFromCompilation(
            IAssemblyLoadContext loader,
            CodeAnalysis.Compilation compilation)
        {
            EmitResult result;
            using (var ms = new MemoryStream())
            {
                using (var pdb = new MemoryStream())
                {
                    if (PlatformHelper.IsMono)
                    {
                        result = compilation.Emit(ms, pdbStream: null);
                    }
                    else
                    {
                        result = compilation.Emit(ms, pdbStream: pdb);
                    }

                    if (!result.Success)
                    {
                        var formatter = new DiagnosticFormatter();
                        var errorMessages = result.Diagnostics
                                             .Where(IsError)
                                             .Select(d => formatter.Format(d));

                        return CompilationResult.FromErrorMessages(errorMessages);
                    }

                    ms.Seek(0, SeekOrigin.Begin);

                    Assembly assembly;
                    if (PlatformHelper.IsMono)
                    {
                        assembly = loader.LoadStream(ms, assemblySymbols: null);
                    }
                    else
                    {
                        pdb.Seek(0, SeekOrigin.Begin);
                        assembly = loader.LoadStream(ms, pdb);
                    }

                    return CompilationResult.FromAssembly(assembly);
                }
            }
        }

        private static bool IsError(Diagnostic diagnostic)
        {
            return diagnostic.IsWarningAsError || diagnostic.Severity == DiagnosticSeverity.Error;
        }
    }
}