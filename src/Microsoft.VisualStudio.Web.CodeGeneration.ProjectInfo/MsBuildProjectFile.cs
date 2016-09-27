using System.Collections.Generic;

namespace Microsoft.VisualStudio.Web.CodeGeneration.ProjectInfo
{ 
    public class MsBuildProjectFile
    {
        private readonly IEnumerable<string> _sourceFiles;
        private readonly IEnumerable<string> _projectReferences;
        private readonly IEnumerable<string> _assemblyReferences;
        private readonly IDictionary<string, string> _properties;
        private readonly string _fullPath;

        public MsBuildProjectFile(string fullPath,
            IEnumerable<string> sourceFiles,
            IEnumerable<string> projectReferences,
            IEnumerable<string> assemblyReferences,
            IDictionary<string, string> properties)
        {
            Requires.NotNullOrEmpty(fullPath);
            Requires.NotNull(sourceFiles);
            Requires.NotNull(projectReferences);
            Requires.NotNull(assemblyReferences);
            Requires.NotNull(properties);

            _sourceFiles = sourceFiles;
            _projectReferences = projectReferences;
            _assemblyReferences = assemblyReferences;
            _properties = properties;
            _fullPath = fullPath;
        }

        public IEnumerable<string> SourceFiles
        {
            get
            {
                return _sourceFiles;
            }
        }

        public IEnumerable<string> ProjectReferences
        {
            get
            {
                return _projectReferences;
            }
        }

        public IEnumerable<string> AssemblyReferences
        {
            get
            {
                return _assemblyReferences;
            }
        }

        public IDictionary<string, string> GlobalProperties
        {
            get
            {
                return _properties;
            }
        }

        public string FullPath
        {
            get
            {
                return _fullPath;
            }
        }
    }
}
