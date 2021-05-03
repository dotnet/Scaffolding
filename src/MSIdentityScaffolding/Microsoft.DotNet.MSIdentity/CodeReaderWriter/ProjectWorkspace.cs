using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.MSBuild;
using System.Threading.Tasks;

namespace Microsoft.DotNet.MSIdentity.CodeReaderWriter
{
    public class CodeAnaylsisHelper
    {
        private string _projectFilePath;
        public CodeAnaylsisHelper(string projectFilePath)
        {
            _projectFilePath = projectFilePath;
        }

        private CodeAnalysis.Project? _codeAnalysisProject;
        public CodeAnalysis.Project CodeAnalysisProject
        {
            get
            {
                if (_codeAnalysisProject == null)
                {
                    _codeAnalysisProject =  LoadCodeAnalysisProject().Result;
                }
                return _codeAnalysisProject;
            }
        }

        private async Task<CodeAnalysis.Project> LoadCodeAnalysisProject()
        {
            var workspace = MSBuildWorkspace.Create();
            var project = await workspace.OpenProjectAsync(_projectFilePath);
            var projectWithFiles = project.WithAllSourceFiles();
            project = projectWithFiles ?? project;
            return project;
        }
    }
}
