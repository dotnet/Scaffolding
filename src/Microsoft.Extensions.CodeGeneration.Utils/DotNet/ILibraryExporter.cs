using System;
using System.Collections.Generic;
using Microsoft.DotNet.ProjectModel.Compilation;

namespace Microsoft.Extensions.CodeGeneration.DotNet
{
    public interface ILibraryExporter
    {
        IEnumerable<LibraryExport> GetAllExports();
        LibraryExport GetExport(string name);
    }
}
