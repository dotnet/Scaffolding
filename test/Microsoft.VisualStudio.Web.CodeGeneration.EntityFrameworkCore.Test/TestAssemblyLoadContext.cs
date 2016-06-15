using Microsoft.VisualStudio.Web.CodeGeneration.DotNet;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.IO;
using System.Reflection;
using System.Runtime.Loader;

namespace Microsoft.VisualStudio.Web.CodeGeneration.EntityFrameworkCore.Test
{
    public class TestAssemblyLoadContext : ICodeGenAssemblyLoadContext
    {
        private ICodeGenAssemblyLoadContext _defaultContext;
        ILibraryManager _manager;
        ILibraryExporter _exporter;
        public TestAssemblyLoadContext(ILibraryExporter exporter, ILibraryManager manager)
        {
            _manager = manager;
            _exporter = exporter;
            _defaultContext = new DefaultAssemblyLoadContext();
        }
        public Assembly LoadFromName(AssemblyName AssemblyName)
        {
            var library = _manager.GetLibrary(AssemblyName.Name);
            var path = _exporter.GetResolvedPathForDependency(library);
            return AssemblyLoadContext.Default.LoadFromAssemblyPath(path);
        }

        public Assembly LoadStream(Stream assembly, Stream symbols)
        {
            return _defaultContext.LoadStream(assembly, symbols);
        }
    }
}
