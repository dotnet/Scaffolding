// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Reflection;

namespace Microsoft.Framework.CodeGeneration
{
    internal class CompilationResult
    {
        public bool Success { get; set; }

        public Assembly Assembly { get; set; }

        public IEnumerable<string> ErrorMessages { get; set; }

        public static CompilationResult FromAssembly([NotNull]Assembly assembly)
        {
            return new CompilationResult()
            {
                Assembly = assembly,
                Success = true
            };
        }

        public static CompilationResult FromErrorMessages([NotNull]IEnumerable<string> errorMessages)
        {
            return new CompilationResult()
            {
                ErrorMessages = errorMessages,
                Success = false
            };
        }
    }
}