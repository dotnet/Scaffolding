// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Reflection;

namespace Microsoft.VisualStudio.Web.CodeGeneration
{
    public class CompilationResult
    {
        public bool Success { get; set; }

        public Assembly Assembly { get; set; }

        public IEnumerable<string> ErrorMessages { get; set; }

        public static CompilationResult FromAssembly(Assembly assembly)
        {
            if (assembly == null)
            {
                throw new ArgumentNullException(nameof(assembly));
            }

            return new CompilationResult()
            {
                Assembly = assembly,
                Success = true
            };
        }

        public static CompilationResult FromErrorMessages(IEnumerable<string> errorMessages)
        {
            if (errorMessages == null)
            {
                throw new ArgumentNullException(nameof(errorMessages));
            }

            return new CompilationResult()
            {
                ErrorMessages = errorMessages,
                Success = false
            };
        }
    }
}
