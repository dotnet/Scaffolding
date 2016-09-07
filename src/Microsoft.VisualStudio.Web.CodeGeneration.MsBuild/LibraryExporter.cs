using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.Extensions.DependencyModel;

namespace Microsoft.VisualStudio.Web.CodeGeneration.MsBuild
{
    public class LibraryExporter
    {
        private string _depsFile;
        private DependencyContext _dependencyContext = null;
        public LibraryExporter(MsBuildProjectContext msBuildProjectContext)
        {
            if (msBuildProjectContext == null)
            {
                throw new ArgumentNullException(nameof(msBuildProjectContext));
            }

            _depsFile = msBuildProjectContext.DepsJson;
            if (string.IsNullOrEmpty(_depsFile)) 
            {
                throw new ArgumentException("Project may not be resotred yet"); 
            }
            Init();
        }

        private void Init()
        {
            if (File.Exists(_depsFile))
            {
                using (var stream = File.OpenRead(_depsFile))
                {
                    _dependencyContext = new DependencyContextJsonReader().Read(stream);
                }
            }
        }

        public Library GetExport(string name)
        {
            if (string.IsNullOrEmpty(name))
            {
                throw new ArgumentNullException(nameof(name));
            }

            return _dependencyContext?.CompileLibraries?.FirstOrDefault(l => l.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
        }

        public IEnumerable<Library> GetExports()
        {

            return _dependencyContext?.CompileLibraries;
        }

        public IEnumerable<Library> GetReferencingLibraries(string name)
        {
            return _dependencyContext.CompileLibraries.Where(l => HasDependency(l, name));
        }

        private bool HasDependency(CompilationLibrary l, string name)
        {
            return l.Dependencies.Any(d => d.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
        }

    }
}
