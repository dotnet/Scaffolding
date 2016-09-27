using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Microsoft.VisualStudio.Web.CodeGeneration.MsBuild
{
    public class ProjectDependencyProvider
    {
        public ProjectDependencyProvider(Dictionary<string, DependencyDescription> nugetPackages, IEnumerable<ResolvedReference> resolvedReferences)
        {
            Requires.NotNull(nugetPackages);
            Requires.NotNull(resolvedReferences);

            NugetPackages = nugetPackages;
            ResolvedReferences = resolvedReferences;
        }
        private Dictionary<string,DependencyDescription> NugetPackages { get; }
        private IEnumerable<ResolvedReference> ResolvedReferences { get; }

        public DependencyDescription GetPackage(string name)
        {
            var dependency = NugetPackages
                ?.Where(p => p.Value.Name.Equals(name, StringComparison.OrdinalIgnoreCase))
                .FirstOrDefault();
            return dependency?.Value;
        }

        public IEnumerable<DependencyDescription> GetAllPackages()
        {
            return NugetPackages
                ?.Select(p => p.Value);
        }
    }
}
