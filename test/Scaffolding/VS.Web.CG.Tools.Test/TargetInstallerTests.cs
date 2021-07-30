// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using Microsoft.DotNet.Scaffolding.Shared;
using Xunit;

namespace Microsoft.VisualStudio.Web.CodeGeneration.Tools.Test
{
    public class TargetInstallerTests
    {
        [Fact]
        public void TestTargetInstallation()
        {
            var installer = new TargetInstaller(new ConsoleLogger());
            var targetLocation = Path.Combine(Path.GetTempPath(), new Guid().ToString());
            installer.EnsureTargetImported("test.csproj", targetLocation);
            var expectedFilePath = Path.Combine(targetLocation, "test.csproj.codegeneration.targets");
            Assert.True(File.Exists(expectedFilePath));
        }
    }
}
