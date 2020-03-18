// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using System.Reflection;
using Microsoft.VisualStudio.Web.CodeGeneration.Msbuild;
using Moq;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.VisualStudio.Web.CodeGeneration.MSBuild.Test
{
    public class ProjectReferenceInformationTests
    {
        private const string ProjectReference1 = @"
<Project Sdk=""Microsoft.NET.Sdk"">
  <PropertyGroup>
    <RootNamespace>Microsoft.Library</RootNamespace>
    <ProjectName>Library1</ProjectName>
    <OutputType>Library</OutputType>
    <TargetFramework>net5.0</TargetFramework>
    <OutputPath>bin\$(Configuration)</OutputPath>
  </PropertyGroup>

  <ItemGroup>
    <Compile Include=""**\*.cs"" Exclude=""Excluded.cs"" />
  </ItemGroup>

  <ItemGroup>
    <ProjectReference Include=""..\Library2\Library2.csproj"" />
  </ItemGroup>
  <ItemGroup Condition=""'$(TargetFramework)' == 'netcoreapp5.0' "">
    <Reference Include=""System"" />
    <Reference Include=""System.Data"" />
  </ItemGroup>
</Project>";
        private ITestOutputHelper _output;

        public ProjectReferenceInformationTests(ITestOutputHelper output)
        {
            _output = output;
        }

        [Theory (Skip="Could not determine a valid location to Msbuild")]
        [InlineData("Root/Root.csproj", ".", "../Library1/Library1.csproj")]
        [InlineData("src/Root/Root.csproj", ".", "../../Library1/Library1.csproj")]
        public void TestGetProjectReferenceInformation(string rootProjectPath, string libraryDirectoryPath, string relativePathToLibrary)
        {
            using(var temporaryFileProvider = new TemporaryFileProvider())
            {
                rootProjectPath = Path.Combine(temporaryFileProvider.Root, rootProjectPath);
                Directory.CreateDirectory(Path.Combine(
                    temporaryFileProvider.Root,
                     libraryDirectoryPath,
                     "Library1"));
                Directory.CreateDirectory(Path.Combine(
                    temporaryFileProvider.Root,
                    libraryDirectoryPath,
                     "Library2"));

                AddProject(
                    $"{libraryDirectoryPath}/Library1/Library1.csproj",
                    ProjectReference1,
                    temporaryFileProvider);
                AddProject(
                    $"{libraryDirectoryPath}/Library2/Library2.csproj",
                    ProjectReference1,
                    temporaryFileProvider);
                var projectReferenceInformation = ProjectReferenceInformationProvider.GetProjectReferenceInformation(
                    rootProjectPath,
                    new string[] {relativePathToLibrary});

                Assert.NotNull(projectReferenceInformation);
            }
        }

        private void AddProject(string projectPath, string projectContents, TemporaryFileProvider fileProvider)
        {
            fileProvider.Add(projectPath, projectContents);
        }
    }
}
