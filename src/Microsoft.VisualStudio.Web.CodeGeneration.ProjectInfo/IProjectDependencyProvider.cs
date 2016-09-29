using System.Collections.Generic;

namespace Microsoft.VisualStudio.Web.CodeGeneration.ProjectInfo
{
    public interface IProjectDependencyProvider
    {
        IEnumerable<DependencyDescription> GetAllPackages();
        IEnumerable<ResolvedReference> GetAllResolvedReferences();
        DependencyDescription GetPackage(string name);
        IEnumerable<DependencyDescription> GetReferencingPackages(string name);
        ResolvedReference GetResolvedReference(string name);
    }
}