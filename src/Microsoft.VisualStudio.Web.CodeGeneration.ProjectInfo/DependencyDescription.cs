using System;
using System.Collections.Generic;
using System.Linq;

namespace Microsoft.VisualStudio.Web.CodeGeneration.ProjectInfo
{
    public class DependencyDescription
    {
        private HashSet<Dependency> _dependencies;
        public DependencyDescription(string name, string path, string itemSpec, string version, string type, string resolved)
        {
            Requires.NotNullOrEmpty(name);
            Requires.NotNullOrEmpty(itemSpec);

            Name = name;
            Path = path;
            ItemSpec = itemSpec;
            Version = version;

            bool res = false;
            Resolved = bool.TryParse(resolved, out res) ? res : false;

            DependencyType d;
            Type = Enum.TryParse(type, out d) ? d :DependencyType.Unknown;

            _dependencies = new HashSet<Dependency>();
        }

        public string TFM
        {
            get
            {
                return ItemSpec.Split('/').FirstOrDefault();
            }
        }
        public string Name { get; }
        public string Path { get; }
        public string ItemSpec { get; }
        public string Version { get; }
        public DependencyType Type { get; }
        public bool Resolved { get; }


        public IEnumerable<Dependency> Dependencies
        {
            get
            {
                return _dependencies;
            }
        }

        public void AddDependency(Dependency d)
        {
            Requires.NotNull(d);
            if (!_dependencies.Contains(d))
            {
                _dependencies.Add(d);
            }
        }
    }
}
