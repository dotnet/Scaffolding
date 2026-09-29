// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using System;
using System.IO;
using Microsoft.Build.Locator;
using Microsoft.DotNet.Scaffolding.Internal.Services;
using Microsoft.DotNet.Tools.Scaffold.AspNet.Helpers;
using Xunit;

namespace Microsoft.DotNet.Tools.Scaffold.Tests.AspNet.Helpers;

public class BlazorWebAssemblyClientProjectResolverTests : IDisposable
{
    private readonly string _directory = Path.Combine(Path.GetTempPath(), nameof(BlazorWebAssemblyClientProjectResolverTests), Guid.NewGuid().ToString());
    private readonly IFileSystem _fileSystem = FileSystem.Instance;

    public BlazorWebAssemblyClientProjectResolverTests()
    {
        if (!MSBuildLocator.IsRegistered)
        {
            MSBuildLocator.RegisterDefaults();
        }

        Directory.CreateDirectory(_directory);
    }

    [Fact]
    public void TryGetClient_ResolvesWebAssemblySdkClientWithoutRestore()
    {
        var server = WriteProject("Server.csproj", """
            <Project Sdk="Microsoft.NET.Sdk.Web">
              <PropertyGroup><TargetFramework>net10.0</TargetFramework></PropertyGroup>
              <ItemGroup>
                <ProjectReference Include="Client/Client.csproj" />
                <ProjectReference Include="Client/Client.csproj" />
                <ProjectReference Include="Library/Library.csproj" />
              </ItemGroup>
            </Project>
            """);
        var client = WriteProject(Path.Combine("Client", "Client.csproj"), """
            <Project Sdk="Microsoft.NET.Sdk.BlazorWebAssembly">
              <PropertyGroup>
                <TargetFramework>net10.0</TargetFramework>
                <RootNamespace>Custom.Client.Root</RootNamespace>
              </PropertyGroup>
            </Project>
            """);
        WriteProject(Path.Combine("Library", "Library.csproj"), "<Project />");

        var serverAssetsPath = Path.Combine(_directory, "obj", "project.assets.json");
        var clientAssetsPath = Path.Combine(_directory, "Client", "obj", "project.assets.json");
        Assert.False(File.Exists(serverAssetsPath));
        Assert.False(File.Exists(clientAssetsPath));
        Assert.True(BlazorWebAssemblyClientProjectResolver.TryGetClient(server, _fileSystem, out var clientProject, out var error), error);
        Assert.Null(error);
        Assert.NotNull(clientProject);
        Assert.Equal(client, clientProject.Value.ProjectPath);
        Assert.Equal("Custom.Client.Root", clientProject.Value.RootNamespace);
        Assert.False(File.Exists(serverAssetsPath));
        Assert.False(File.Exists(clientAssetsPath));
    }

    [Fact]
    public void TryGetClient_ReturnsNullForServerWithoutClient()
    {
        var server = WriteProject("Server.csproj", """
            <Project><ItemGroup><ProjectReference Include="Library.csproj" /></ItemGroup></Project>
            """);
        WriteProject("Library.csproj", "<Project />");

        Assert.True(BlazorWebAssemblyClientProjectResolver.TryGetClient(server, _fileSystem, out var client, out var error), error);
        Assert.Null(client);
        Assert.Null(error);
    }

    [Fact]
    public void TryGetClient_UsesProjectNameWhenRootNamespaceIsUnset()
    {
        var server = WriteProject("Server.csproj", """
            <Project><ItemGroup><ProjectReference Include="MyApp.Client.csproj" /></ItemGroup></Project>
            """);
        var clientPath = WriteProject("MyApp.Client.csproj", """
            <Project><PropertyGroup>
              <UsingMicrosoftNETSdkBlazorWebAssembly>true</UsingMicrosoftNETSdkBlazorWebAssembly>
            </PropertyGroup></Project>
            """);

        Assert.True(BlazorWebAssemblyClientProjectResolver.TryGetClient(server, _fileSystem, out var client, out var error), error);
        Assert.NotNull(client);
        Assert.Equal(clientPath, client.Value.ProjectPath);
        Assert.Equal("MyApp.Client", client.Value.RootNamespace);
    }

    [Fact]
    public void TryGetClient_ReportsAmbiguousClients()
    {
        var server = WriteProject("Server.csproj", """
            <Project><ItemGroup>
              <ProjectReference Include="First.csproj" />
              <ProjectReference Include="Second.csproj" />
            </ItemGroup></Project>
            """);
        const string clientProject = """
            <Project><PropertyGroup>
              <UsingMicrosoftNETSdkBlazorWebAssembly>true</UsingMicrosoftNETSdkBlazorWebAssembly>
            </PropertyGroup></Project>
            """;
        WriteProject("First.csproj", clientProject);
        WriteProject("Second.csproj", clientProject);

        Assert.False(BlazorWebAssemblyClientProjectResolver.TryGetClient(server, _fileSystem, out var client, out var error));
        Assert.Null(client);
        Assert.Contains("First.csproj", error);
        Assert.Contains("Second.csproj", error);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void TryGetClient_ReportsProjectEvaluationFailure(bool serverFails)
    {
        var server = WriteProject("Server.csproj", serverFails
            ? """<Project><Import Project="Missing.props" /></Project>"""
            : """<Project><ItemGroup><ProjectReference Include="Client.csproj" /></ItemGroup></Project>""");
        if (!serverFails)
        {
            WriteProject("Client.csproj", """<Project><Import Project="Missing.props" /></Project>""");
        }

        Assert.False(BlazorWebAssemblyClientProjectResolver.TryGetClient(server, _fileSystem, out var client, out var error));
        Assert.Null(client);
        Assert.Contains("Missing.props", error);
    }

    private string WriteProject(string relativePath, string contents)
    {
        var path = Path.Combine(_directory, relativePath);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, contents);
        return path;
    }

    public void Dispose() => Directory.Delete(_directory, recursive: true);
}
