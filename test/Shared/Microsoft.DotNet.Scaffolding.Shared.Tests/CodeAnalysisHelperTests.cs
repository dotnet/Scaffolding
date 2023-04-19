// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using Microsoft.DotNet.Scaffolding.Shared.CodeModifier;
using Xunit;

namespace Microsoft.DotNet.Scaffolding.Shared.Tests
{
    public class CodeAnalysisHelperTests
    {
        [Fact]
        public void LoadCodeAnalysisProjectTest()
        {
            var files = new List<string>();
            var projectPath = string.Empty; // TODO test project
            var project = CodeAnalysisHelper.LoadCodeAnalysisProject(projectPath, files);
            Assert.NotNull(project);
        }
    }
}
