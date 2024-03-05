// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.DotNet.Scaffolding.Helpers.Services
{
    public class ProjectService : IProjectService
    {
        private readonly string _projectPath;
        public ProjectService(string projectPath)
        {
            _projectPath = projectPath ?? throw new ArgumentNullException(nameof(projectPath));
        }

        public IList<string> GetItemValues()
        {
            throw new NotImplementedException();
        }

        public IList<string> GetPropertyValues()
        {
            throw new NotImplementedException();
        }

        public void Setup()
        {

            //var msbuildThang = new DefaultMsbuildProjectAccess(_projectPath, NullLogger.Instance, CancellationToken.None);
            //var projectStuff = msbuildThang.Project;
        }
    }
}
