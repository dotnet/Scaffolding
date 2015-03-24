// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using Microsoft.Framework.Runtime;
using Moq;
using Xunit;

namespace Microsoft.Framework.CodeGeneration.Core.Test
{
    public class PackageInstallerTests
    {
        private Mock<ILogger> _mockLogger;
        private Mock<IApplicationEnvironment> _mockApp;
        private MockFileSystem _mockFileSystem;
        private PackageInstaller _packageInstaller;
        private string _projectJsonPath;

        public PackageInstallerTests()
        {
            _mockLogger = new Mock<ILogger>();
            _mockApp = new Mock<IApplicationEnvironment>();
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

            Assert.Equal(expectedJson, actualJson);
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

            Assert.Equal(expectedJson, actualJson);
        }
    }
}
