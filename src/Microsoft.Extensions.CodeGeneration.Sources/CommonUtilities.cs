// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.IO;
using System.Linq;
using System.Reflection;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Emit;
using Microsoft.Extensions.CodeGeneration.DotNet;
using System;

namespace Microsoft.Extensions.CodeGeneration
{
    internal static class CommonUtilities
    {
        public static CompilationResult GetAssemblyFromCompilation(
            ICodeGenAssemblyLoadContext loader,
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
                    //TODO: @prbhosal Should consider removing this as Mono is no longer supported.                  
                    if (PlatformHelper.IsMono)
                    {
                        assembly = loader.LoadStream(ms, symbols: null);
                    }
                    else
                    {
                        try
                        {
                            pdb.Seek(0, SeekOrigin.Begin);
                            assembly = loader.LoadStream(ms, pdb);
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine(ex.Message);
                            var v = ex;
                            while (v.InnerException != null)
                            {
                                v = v.InnerException;
                                Console.WriteLine(v.Message);
                            }
                            throw ex;
                        }
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