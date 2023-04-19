// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Threading.Tasks;

namespace Microsoft.VisualStudio.Web.CodeGeneration.Templating.Compilation
{
    public interface ICompilationService
    {
        CompilationResult Compile(string content);
    }
}
