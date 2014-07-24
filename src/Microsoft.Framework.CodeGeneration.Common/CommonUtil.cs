// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Emit;
using Microsoft.Framework.Runtime;

namespace Microsoft.Framework.CodeGeneration
{
    internal static class CommonUtil
    {
        public static bool TryGetAssemblyFromCompilation(IAssemblyLoaderEngine loader,
            Compilation compilation, out Assembly assembly, out IEnumerable<string> errorMessages)
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
                        assembly = null;

                        var formatter = new DiagnosticFormatter();
                        errorMessages = result.Diagnostics
                                             .Where(IsError)
                                             .Select(d => formatter.Format(d));

                        return false;
                    }

                    ms.Seek(0, SeekOrigin.Begin);

                    if (PlatformHelper.IsMono)
                    {
                        assembly = loader.LoadStream(ms, pdbStream: null);
                    }
                    else
                    {
                        pdb.Seek(0, SeekOrigin.Begin);
                        assembly = loader.LoadStream(ms, pdb);
                    }

                    errorMessages = null;
                    return true;
                }
            }
        }

        private static bool IsError(Diagnostic diagnostic)
        {
            return diagnostic.IsWarningAsError || diagnostic.Severity == DiagnosticSeverity.Error;
        }
    }
}