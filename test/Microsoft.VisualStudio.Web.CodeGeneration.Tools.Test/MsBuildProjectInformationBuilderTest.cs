// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.DotNet.Cli.Utils;
using Microsoft.VisualStudio.Web.CodeGeneration.Tools.Internal;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.VisualStudio.Web.CodeGeneration.Tools.Test
{
    public class MsBuildProjectInformationBuilderTest
    {
        private const string NugetConfigTxt = @"
<configuration>
    <packageSources>
        <clear />
        <add key=""NuGet"" value=""https://api.nuget.org/v3/index.json"" />
        <add key=""dotnet-core"" value=""https://dotnet.myget.org/F/dotnet-core/api/v3/index.json"" />
        <add key=""dotnet-buildtools"" value=""https://dotnet.myget.org/F/dotnet-buildtools/api/v3/index.json"" />
        <add key=""nugetbuild"" value=""https://www.myget.org/F/nugetbuild/api/v3/index.json"" />
    </packageSources>
</configuration>";

        private const string RootProjectTxt = @"
<Project ToolsVersion=""14.0"" xmlns=""http://schemas.microsoft.com/developer/msbuild/2003"">
  <Import Project=""$(MSBuildExtensionsPath)\$(MSBuildToolsVersion)\Microsoft.Common.props"" />

  <PropertyGroup>
    <RootNamespace>Microsoft.TestProject</RootNamespace>
    <ProjectName>TestProject</ProjectName>
    <OutputType>Library</OutputType>
    <TargetFrameworks>netcoreapp1.0</TargetFrameworks>
    <OutputPath>bin\$(Configuration)</OutputPath>
  </PropertyGroup>

  <ItemGroup>
    <Compile Include=""**\*.cs"" Exclude=""Excluded.cs"" />
  </ItemGroup>

  <ItemGroup>
    <PackageReference Include=""Microsoft.AspNetCore.Mvc"">
      <Version>1.0.0-*</Version>
    </PackageReference>
    <PackageReference Include=""Microsoft.NETCore.Sdk"">
      <Version>1.0.0-*</Version>
    </PackageReference>
    <PackageReference Include=""Microsoft.NETCore.App"">
      <Version>1.0.1</Version>
    </PackageReference>
  </ItemGroup>
  <ItemGroup>
    <ProjectReference Include=""..\Library1\Library1.csproj"" />
  </ItemGroup>
  <ItemGroup>
    <Reference Include = ""xyz.dll"" />
  </ItemGroup>
  <Import Project=""$(MSBuildToolsPath)\Microsoft.CSharp.targets"" />
</Project>
";

        private const string LibraryProjectTxt = @"
<Project ToolsVersion=""14.0"" xmlns=""http://schemas.microsoft.com/developer/msbuild/2003"">
  <Import Project=""$(MSBuildExtensionsPath)\$(MSBuildToolsVersion)\Microsoft.Common.props"" />

  <PropertyGroup>
    <RootNamespace>Microsoft.Library</RootNamespace>
    <ProjectName>Library1</ProjectName>
    <OutputType>Library</OutputType>
    <TargetFrameworks>netcoreapp1.0</TargetFrameworks>
    <OutputPath>bin\$(Configuration)</OutputPath>
  </PropertyGroup>

  <ItemGroup>
    <Compile Include=""**\*.cs"" Exclude=""Excluded.cs"" />
  </ItemGroup>

  <ItemGroup>
    <PackageReference Include=""Microsoft.NETCore.Sdk"">
      <Version>1.0.0-*</Version>
    </PackageReference>
    <PackageReference Include=""Microsoft.NETCore.App"">
      <Version>1.0.1</Version>
    </PackageReference>
  </ItemGroup>
  <Import Project=""$(MSBuildToolsPath)\Microsoft.CSharp.targets"" />
</Project>
";
        private ITestOutputHelper _output;
        public MsBuildProjectInformationBuilderTest(ITestOutputHelper output)
        {
            _output = output;
        }

        [Fact(Skip = "Need newer version of sdk that has all msbuild related fixes")]
        public void TestProjectInformationCreation()
        {
            using (var fileProvider = new TemporaryFileProvider())
            {
                Directory.CreateDirectory(Path.Combine(fileProvider.Root, "Root"));
                Directory.CreateDirectory(Path.Combine(fileProvider.Root, "Library1"));
                fileProvider.Add("Nuget.config", NugetConfigTxt);

                fileProvider.Add("Root/test.csproj", RootProjectTxt);
                fileProvider.Add($"Root/One.cs", "public class Abc {}");
                fileProvider.Add($"Root/Two.cs", "public class Abc2 {}");
                fileProvider.Add($"Root/Excluded.cs", "public class Abc {}");

                fileProvider.Add("Library1/Library1.csproj", LibraryProjectTxt);
                fileProvider.Add($"Library1/Three.cs", "public class Abc3 {}");

                var result = Command.CreateDotNet("restore3",
                    new[] { Path.Combine(fileProvider.Root, "Root", "test.csproj") })
                    .OnErrorLine(l => _output.WriteLine(l))
                    .OnOutputLine(l => _output.WriteLine(l))
                    .Execute();

                Assert.Equal(0, result.ExitCode);

                var projectInformation = new MsBuildProjectInformationBuilder(Path.Combine(fileProvider.Root, "Root", "test.csproj"))
                    .Build();

                Assert.NotNull(projectInformation);
                Assert.NotNull(projectInformation.RootProject);
                Assert.NotNull(projectInformation.DependencyProjects);

                Assert.Equal("test", projectInformation.RootProject.AssemblyName);
                Assert.Equal("Library1", projectInformation.DependencyProjects.First().AssemblyName);
            }
        }
    }
}
