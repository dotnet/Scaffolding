using System;
using System.IO;
using Microsoft.Build.Execution;

namespace Microsoft.VisualStudio.Web.CodeGeneration.MsBuild
{
    public class ResolvedReference
    {
        private const string ItemSpecKey = "OriginalItemSpec";
        private const string NugetSourceTypeKey = "NugetSourceType";
        private const string VersionKey = "Version";

        public ResolvedReference(string name, string itemSpec, string nugetPackageSource, string resolvedPath, string version)
        {
            ItemSpec = itemSpec;
            Name = name;
            ResolvedPath = resolvedPath;
            Version = version;

            IsNugetPackage = !string.IsNullOrEmpty(nugetPackageSource) && "Package".Equals(nugetPackageSource, StringComparison.OrdinalIgnoreCase);
        }

        public string ItemSpec { get; }
        public bool IsNugetPackage { get; }
        public string ResolvedPath { get; }
        public string Name { get; }
        public string Version { get; }
        public bool IsResolved
        {
            get
            {
                return !string.IsNullOrEmpty(ResolvedPath) && File.Exists(ResolvedPath);
            }
        }

        public static ResolvedReference FromProjectItem(ProjectItemInstance item)
        {
            string itemSpec = string.Empty;
            string nugetPackageSource = string.Empty;
            string version = string.Empty;
            string resolvedPath = string.Empty;
            string name = string.Empty;

            resolvedPath = item.EvaluatedInclude;

            name = resolvedPath ?? Path.GetFileNameWithoutExtension(resolvedPath);

            foreach (var m in item.Metadata)
            {
                if (ItemSpecKey.Equals(m.Name, StringComparison.OrdinalIgnoreCase))
                {
                    itemSpec = m.EvaluatedValue;
                    continue;
                }

                if (NugetSourceTypeKey.Equals(m.Name, StringComparison.OrdinalIgnoreCase))
                {
                    nugetPackageSource = m.EvaluatedValue;
                    continue;
                }

                if (VersionKey.Equals(m.Name, StringComparison.OrdinalIgnoreCase))
                {
                    version = m.EvaluatedValue;
                }
            }

            return new ResolvedReference(name, itemSpec, nugetPackageSource, resolvedPath, version);
        }
    }
}
