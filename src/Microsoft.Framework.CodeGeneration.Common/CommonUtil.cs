// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using System.Reflection;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Emit;
using Microsoft.Framework.Runtime;

namespace Microsoft.Framework.CodeGeneration
{
    internal static class CommonUtil
    {
        public static bool TryGetAssemblyFromCompilation(IAssemblyLoaderEngine loader,
            Compilation compilation, out Assembly assembly, out EmitResult result)
        {
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

                    return true;
                }
            }
        }
    }
}