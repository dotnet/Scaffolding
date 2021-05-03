using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Microsoft.Extensions.Internal;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NuGet.ProjectModel;

namespace Microsoft.DotNet.MSIdentity.Project
{
    internal class DependencyGraphService : IDependencyGraphService
    {
        private string? _projectFilePath;
        public DependencyGraphService(string? projectPath = null)
        {
            _projectFilePath = projectPath;
        }

        public DependencyGraphSpec? GenerateDependencyGraph()
        {
            var dependencyGraph = new DependencyGraphSpec();
            var tmpJsonPath = Path.Combine(Path.GetTempPath(), Path.GetTempFileName());
            if (!string.IsNullOrEmpty(tmpJsonPath))
            {
                var errors = new List<string>();
                var output = new List<string>();

                IList<string> arguments = new List<string>();

                //if project path is present, use it for dotnet user-secrets
                if (!string.IsNullOrEmpty(_projectFilePath))
                {
                    arguments.Add(_projectFilePath);
                }

                arguments.Add("/t:GenerateRestoreGraphFile");
                arguments.Add($"/p:RestoreGraphOutputPath={tmpJsonPath}");
                var result = Command.CreateDotNet(
                    "restore",
                    arguments)
                    .OnErrorLine(e => errors.Add(e))
                    .OnOutputLine(o => output.Add(o))
                    .Execute();

                if (result.ExitCode != 0)
                {
                    throw new Exception("\nError while running dotnet restore.\n");
                }
                else
                {
                    dependencyGraph = DependencyGraphSpec.Load(tmpJsonPath);
                }
            }
            return dependencyGraph;
        }
    }
}
