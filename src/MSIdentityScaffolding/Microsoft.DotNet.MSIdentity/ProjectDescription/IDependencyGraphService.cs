using NuGet.ProjectModel;

namespace Microsoft.DotNet.MSIdentity.Project
{
    internal interface IDependencyGraphService
    {
        DependencyGraphSpec? GenerateDependencyGraph();
    }
}
