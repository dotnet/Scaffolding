using Microsoft.DotNet.ProjectModel;
using System;
using System.Collections.Generic;

namespace Microsoft.Extensions.CodeGeneration.DotNet
{
    public interface ILibraryManager
    {
        IEnumerable<LibraryDescription> GetLibraries();
        LibraryDescription GetLibrary(string name);
        IEnumerable<LibraryDescription> GetReferencingLibraries(string name);
    }
}
