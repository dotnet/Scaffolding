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
<Project ToolsVersion=""14.0"" xmlns=""http://schemas.microsoft.com/developer/msbuild/2003"">
  <PropertyGroup>
    <RootNamespace>Microsoft.Library</RootNamespace>
    <ProjectName>Library1</ProjectName>
    <OutputType>Library</OutputType>
    <TargetFrameworks>netstandard1.6;net451</TargetFrameworks>
    <OutputPath>bin\$(Configuration)</OutputPath>
  </PropertyGroup>

  <ItemGroup>
    <Compile Include=""**\*.cs"" Exclude=""Excluded.cs"" />
  </ItemGroup>

  <ItemGroup>
    <PackageReference Include=""Microsoft.NET.Sdk"" Version=""1.0.0-alpha-20161029-1"" />
    <PackageReference Include=""NETStandard.Library"" Version=""1.6"" />
    <ProjectReference Include=""..\Library2\Library2.csproj"" />
  </ItemGroup>
  <ItemGroup Condition=""'$(TargetFramework)' == 'net451' "">
    <Reference Include=""System"" />
    <Reference Include=""System.Data"" />
  </ItemGroup>
  <Import Project=""$(MSBuildToolsPath)\Microsoft.CSharp.targets"" />
</Project>";
        private const string ProjectReference2 = @"
<Project ToolsVersion=""14.0"" xmlns=""http://schemas.microsoft.com/developer/msbuild/2003"">
  <PropertyGroup>
    <RootNamespace>Microsoft.Library</RootNamespace>
    <ProjectName>Library2</ProjectName>
    <OutputType>Library</OutputType>
    <TargetFrameworks>netstandard1.6;net451</TargetFrameworks>
    <OutputPath>bin\$(Configuration)</OutputPath>
  </PropertyGroup>

  <ItemGroup>
    <Compile Include=""**\*.cs"" Exclude=""Excluded.cs"" />
  </ItemGroup>

  <ItemGroup>
    <PackageReference Include=""Microsoft.NET.Sdk"" Version=""1.0.0-alpha-20161029-1"" />
    <PackageReference Include=""NETStandard.Library"" Version=""1.6"" />
  </ItemGroup>
  <ItemGroup Condition=""'$(TargetFramework)' == 'net451' "">
    <Reference Include=""System"" />
    <Reference Include=""System.Data"" />
  </ItemGroup>
  <Import Project=""$(MSBuildToolsPath)\Microsoft.CSharp.targets"" />
</Project>";

        private ITestOutputHelper _output;

        public ProjectReferenceInformationTests(ITestOutputHelper output)
        {
            _output = output;
        }

        [Theory]
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