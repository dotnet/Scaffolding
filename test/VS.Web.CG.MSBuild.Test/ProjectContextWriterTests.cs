
using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.VisualStudio.Web.CodeGeneration.DotNet;
using Microsoft.VisualStudio.Web.CodeGeneration.Sources.Test;
using Xunit;
using Xunit.Abstractions;


namespace Microsoft.VisualStudio.Web.CodeGeneration.MSBuild
{
    public class ProjectContextWriterTests : TestBase
    {
        static string testAppPath = Path.Combine("..", "TestApps", "ModelTypesLocatorTestClassLibrary");
        ICodeGenAssemblyLoadContext loadContext;
        private ITestOutputHelper _outputHelper;

        public ProjectContextWriterTests(ITestOutputHelper outputHelper)
        {
            loadContext = new DefaultAssemblyLoadContext();
            _outputHelper = outputHelper;
        }

        [Fact]
        public void CommonUtilities_TestGetAssemblyFromCompilation_MsBuild()
        {
            using (var fileProvider = new TemporaryFileProvider())
            {

                new MsBuildProjectSetupHelper().SetupEmptyCodeGenerationProject(fileProvider, _outputHelper);
                var path = Path.Combine(fileProvider.Root, MsBuildProjectStrings.RootProjectName);

                var projectContext = GetProjectContext(path, true);

            }
        }
    }
}
