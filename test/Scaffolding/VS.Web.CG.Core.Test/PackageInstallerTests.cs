// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;
using Microsoft.DotNet.Scaffolding.Shared;
using Microsoft.VisualStudio.Web.CodeGeneration.DotNet;
using Microsoft.VisualStudio.Web.CodeGeneration.Test.Sources;
using Moq;
using Xunit;

namespace Microsoft.VisualStudio.Web.CodeGeneration.Core.Test
{
    public class PackageInstallerTests
    {
        private Mock<ILogger> _mockLogger;
        private Mock<IApplicationInfo> _mockApp;
        private MockFileSystem _mockFileSystem;
        private PackageInstaller _packageInstaller;
        private string _projectJsonPath;

        public PackageInstallerTests()
        {
            _mockLogger = new Mock<ILogger>();
            _mockApp = new Mock<IApplicationInfo>();
            _mockFileSystem = new MockFileSystem();

            var applicationBasePath = @"C:\App";
            _projectJsonPath = Path.Combine(applicationBasePath, "project.json");

            _mockApp.Setup(app => app.ApplicationBasePath)
                .Returns(applicationBasePath);

            _packageInstaller = new PackageInstaller(_mockLogger.Object, _mockApp.Object, _mockFileSystem);
        }

        [Fact]
        public void AddPackages_Adds_New_Depedenency()
        {
            string initialJson = @"{
  ""dependencies"": {
    ""Newtonsoft.Json"": ""5.0.8"",
    ""Microsoft.Net.Runtime.Interfaces"": """"
    }
}";
            string expectedJson = @"{
  ""dependencies"": {
    ""Newtonsoft.Json"": ""5.0.8"",
    ""Microsoft.Net.Runtime.Interfaces"": """",
    ""Microsoft.Net.Runtime"": ""1.0.0""
  }
}";

            _mockFileSystem.WriteAllText(_projectJsonPath, initialJson);

            _packageInstaller.AddPackages(new[] { new PackageMetadata()
            {
                Name = "Microsoft.Net.Runtime",
                Version = "1.0.0"
            }});

            var actualJson = _mockFileSystem.ReadAllText(_projectJsonPath);

            Assert.Equal(expectedJson, actualJson, ignoreCase: false, ignoreLineEndingDifferences: true);
        }

        [Fact]
        public void AddPackages_Adds_Depedenency_Section_If_Needed()
        {
            string initialJson = @"{
  ""webroot"": ""wwwRoot"",
  ""frameworks"": {
    ""dnx451"": {}
  }
}";
            string expectedJson = @"{
  ""webroot"": ""wwwRoot"",
  ""frameworks"": {
    ""dnx451"": {}
  },
  ""dependencies"": {
    ""Microsoft.Net.Runtime"": ""1.0.0""
  }
}";

            _mockFileSystem.WriteAllText(_projectJsonPath, initialJson);

            _packageInstaller.AddPackages(new[] { new PackageMetadata()
            {
                Name = "Microsoft.Net.Runtime",
                Version = "1.0.0"
            }});

            var actualJson = _mockFileSystem.ReadAllText(_projectJsonPath);

            Assert.Equal(expectedJson, actualJson, ignoreCase: false, ignoreLineEndingDifferences: true);
        }
    }
}
