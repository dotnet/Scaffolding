// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.DotNet.Scaffolding.Shared.CodeModifier;
using Xunit;

namespace Microsoft.DotNet.Scaffolding.Shared.Tests
{
    public class ProjectExtensionsTests
    {
        [Theory]
        [InlineData("Components\\App.razor", "Components", "App.razor")]
        [InlineData("Components/App.razor", "Components", "App.razor")]
        [InlineData("Components\\_Imports.razor", "Components", "_Imports.razor")]
        [InlineData("Components/_Imports.razor", "Components", "_Imports.razor")]
        [InlineData("Components/Account\\Pages/Login.razor", "Components/Account/Pages", "Login.razor")]
        [InlineData("App.razor", "Components", "App.razor")]
        public async Task GetDocumentFromName_ResolvesRelativePathsWithEitherSeparator(
            string configuredPath, string directory, string fileName)
        {
            var root = Path.Combine(Path.GetTempPath(), $"project-extensions-{Guid.NewGuid():N}");
            using var workspace = new AdhocWorkspace();
            try
            {
                var projectDirectory = Path.Combine(root, "Server");
                var targetDirectory = Path.Combine(projectDirectory,
                    directory.Replace('/', Path.DirectorySeparatorChar));
                Directory.CreateDirectory(targetDirectory);
                var targetPath = Path.Combine(targetDirectory, fileName);
                File.WriteAllText(targetPath, "expected component");
                Directory.CreateDirectory(Path.Combine(root, "Client"));
                File.WriteAllText(Path.Combine(root, "Client", fileName), "wrong project");
                var project = workspace.AddProject(ProjectInfo.Create(ProjectId.CreateNewId(),
                    VersionStamp.Create(), "Server", "Server", LanguageNames.CSharp,
                    filePath: Path.Combine(projectDirectory, "Server.csproj")));

                var document = project.GetDocumentFromName(configuredPath, DefaultFileSystem.Instance);

                Assert.NotNull(document);
                Assert.Equal(targetPath, document.Name);
                Assert.Equal("expected component", (await document.GetTextAsync()).ToString());
            }
            finally
            {
                if (Directory.Exists(root))
                {
                    Directory.Delete(root, recursive: true);
                }
            }
        }
    }
}
