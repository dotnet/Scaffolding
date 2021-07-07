using NuGet.ProjectModel;

namespace Microsoft.DotNet.MSIdentity.Project
{
    //Interface responsible for any build/restore operations and
    //for getting the dependecy graph for a given project.
    internal interface IDependencyGraphService
    {
        /// <summary>
        /// Generates Nuget.ProjectModel's DependencyGraphSpec from a dotnet restore operation.
        /// Used to check what packages exist in the project. 
        /// </summary>
        /// <returns></returns>
        DependencyGraphSpec? GenerateDependencyGraph();
    }
}
