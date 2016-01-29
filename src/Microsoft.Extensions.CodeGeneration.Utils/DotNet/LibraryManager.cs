using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.DotNet.ProjectModel;

namespace Microsoft.Extensions.CodeGeneration.DotNet
{
    public class LibraryManager : ILibraryManager
    {
        private Microsoft.DotNet.ProjectModel.Resolution.LibraryManager _libraryManager;

        public LibraryManager(ProjectContext projectContext)
        {
            if (projectContext == null)
            {
                throw new ArgumentNullException(nameof(projectContext));
            }
            _libraryManager = projectContext.LibraryManager;
        }

        public IEnumerable<LibraryDescription> GetLibraries()
        {
            return _libraryManager.GetLibraries();
        }

        public LibraryDescription GetLibrary(string name)
        {
            return _libraryManager.GetLibraries().Where(_ => _.Identity.Name == name).FirstOrDefault();
        }

        public IEnumerable<LibraryDescription> GetReferencingLibraries(string name)
        {
            if(name == null)
            {
                throw new ArgumentNullException(nameof(name));
            }
            Console.WriteLine("Getting libraries referencing: " + name);
            // Get all libraries where the dependencies of the library contains 'name' as a dependency.
            return _libraryManager.GetLibraries().Where(_ => HasDependency(_, name));

        }
        private bool HasDependency(LibraryDescription lib, string name)
        {
            return lib.Dependencies.Where(_ => _.Name == name).Any();
        }
    }
}

