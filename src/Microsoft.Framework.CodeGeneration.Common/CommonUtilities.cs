// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.IO;
using System.Linq;
using System.Reflection;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Emit;

namespace Microsoft.Framework.CodeGeneration
{
    internal static class CommonUtilities
    {
        public static CompilationResult GetAssemblyFromCompilation(
            Compilation compilation)
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
                        var assemblyLoadMethod = typeof(Assembly).GetTypeInfo().GetDeclaredMethods("Load")
                            .First(
                                m =>
                                {
                                    var parameters = m.GetParameters();
                                    return parameters.Length == 1 && parameters[0].ParameterType == typeof(byte[]);
                                });
                        assembly = (Assembly)assemblyLoadMethod.Invoke(null, new[] { ms.ToArray() });
                    }
                    else
                    {
                        pdb.Seek(0, SeekOrigin.Begin);

                        var assemblyLoadMethod = typeof(Assembly).GetTypeInfo().GetDeclaredMethods("Load")
                            .First(
                                m =>
                                {
                                    var parameters = m.GetParameters();
                                    return parameters.Length == 2
                                        && parameters[0].ParameterType == typeof(byte[])
                                        && parameters[1].ParameterType == typeof(byte[]);
                                });
                        assembly = (Assembly)assemblyLoadMethod.Invoke(null, new[] { ms.ToArray(), pdb.ToArray() });
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