// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using System.Linq;
using System.Reflection;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Emit;
using Microsoft.Extensions.CodeGeneration.DotNet;

namespace Microsoft.Extensions.CodeGeneration
{
    public static class CommonUtilities
    {
        public static CompilationResult GetAssemblyFromCompilation(
            ICodeGenAssemblyLoadContext loader,
            CodeAnalysis.Compilation compilation)
        {
            using (var ms = new MemoryStream())
            {
                var result = compilation.Emit(ms, pdbStream: null);

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
                try
                {
                    assembly = loader.LoadStream(ms, symbols: null);
                }
                catch (Exception ex)
                {
                    var v = ex;
                    while (v.InnerException != null)
                    {
                        v = v.InnerException;
                    }
                    throw ex;
                }

                return CompilationResult.FromAssembly(assembly);
            }
        }

        private static bool IsError(Diagnostic diagnostic)
        {
            return diagnostic.IsWarningAsError || diagnostic.Severity == DiagnosticSeverity.Error;
        }
    }
}