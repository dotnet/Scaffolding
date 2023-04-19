// Copyright (c) .NET Foundation. All rights reserved.

using System.Threading.Tasks;

namespace Microsoft.VisualStudio.Web.CodeGeneration.Templating.Compilation
{
    public interface ICompilationService
    {
        CompilationResult Compile(string content);
    }
}
