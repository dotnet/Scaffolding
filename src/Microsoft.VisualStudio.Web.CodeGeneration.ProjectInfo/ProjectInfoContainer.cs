
namespace Microsoft.VisualStudio.Web.CodeGeneration.ProjectInfo
{
    public class ProjectInfoContainer
    {
        public IProjectDependencyProvider ProjectDependencyProvider { get; set; }
        public IMsBuildProjectContext ProjectContext { get; set; }
    }
}
