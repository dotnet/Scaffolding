using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Build.Framework;

namespace Microsoft.VisualStudio.Web.CodeGeneration.MsBuild
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

        internal void AddDependency(Dependency d)
        {
            Requires.NotNull(d);
            if (!_dependencies.Contains(d))
            {
                _dependencies.Add(d);
            }
        }

        public static DependencyDescription FromTaskItem(ITaskItem item)
        {
            Requires.NotNull(item);

            
            var version = item.GetMetadata("Version");
            var path = item.GetMetadata("Path");
            var type = item.GetMetadata("Type");
            var resolved = item.GetMetadata("Resolved");
            var itemSpec = item.ItemSpec;

            // For type == Target, we do not get Name in the metadata. This is a special node where the dependencies are 
            // the direct dependencies of the project.
            var name = ("Target".Equals(type, StringComparison.OrdinalIgnoreCase))
                ? itemSpec
                : item.GetMetadata("Name");

            if (string.IsNullOrEmpty(name))
            {
                return null;
            }

            return new DependencyDescription(name, path, itemSpec, version, type, resolved);
        }
    }
}
