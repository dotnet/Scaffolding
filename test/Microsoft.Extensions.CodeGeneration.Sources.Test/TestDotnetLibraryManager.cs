using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.DotNet.ProjectModel;

namespace Microsoft.Extensions.CodeGeneration.Sources.Test
{
    public class TestDotnetLibraryManager : Microsoft.DotNet.ProjectModel.Resolution.LibraryManager
    {
        IList<LibraryDescription> _libraries;
        IList<DiagnosticMessage> _diagnostics;
        string _projectPath;

        public TestDotnetLibraryManager(IList<LibraryDescription> libraries, IList<DiagnosticMessage> diagnostics, string projectPath) : base(libraries, diagnostics, projectPath)
        {
            _libraries = libraries;
            _diagnostics = diagnostics;
            _projectPath = projectPath;
        }

        public new IEnumerable<LibraryDescription> GetLibraries()
        {
            return _libraries;
        }
    }
}
