using System;
using System.Collections.Generic;

namespace Microsoft.VisualStudio.Web.CodeGeneration.ProjectInfo
{
    public class Dependency
    {
        public Dependency(string name, string version, string itemSpec)
        {
            Requires.NotNullOrEmpty(name);
            Requires.NotNullOrEmpty(itemSpec);
            Name = name;
            Version = version;
            ItemSpec = itemSpec;
        }

        public string Name { get; }
        public string Version { get; }
        public string ItemSpec { get; }

        public override int GetHashCode()
        {
            return Name.GetHashCode() ^ (Version == null ? 0 : Version.GetHashCode());
        }

        public override bool Equals(object obj)
        {
            if (obj == null)
            {
                return false;
            }

            var other = obj as Dependency;
            return other != null && other.Name == this.Name && other.Version == this.Version;
        }
    }
}
