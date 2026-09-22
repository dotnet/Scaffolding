// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;

namespace Microsoft.DotNet.Tools.Scaffold.Tests.Helpers;

internal sealed class BlazorTestProject : IDisposable
{
    private readonly string _targetFramework;

    public string DirectoryPath { get; } = Path.Combine(Path.GetTempPath(), "Blazor", Guid.NewGuid().ToString());
    public string ProjectDirectory { get; }
    public string ProjectPath => Path.Combine(ProjectDirectory, "TestProject.csproj");
    public string ClientDirectory => Path.Combine(DirectoryPath, "Client");
    public string ClientProjectPath => Path.Combine(ClientDirectory, "TestProject.Client.csproj");

    public BlazorTestProject(string targetFramework)
    {
        _targetFramework = targetFramework;
        ProjectDirectory = ScaffoldCliHelper.SetupTestProject(DirectoryPath, targetFramework);
        File.WriteAllText(ProjectPath, File.ReadAllText(ProjectPath).Replace(
            "</PropertyGroup>", "  <Nullable>enable</Nullable>\n  </PropertyGroup>"));
        File.WriteAllText(Path.Combine(DirectoryPath, "NuGet.config"), ScaffoldCliHelper.PreviewNuGetConfig);
    }

    public void AddWebAssemblyClient(string aspNetCoreVersion, bool usesInteractiveServer, string clientNamespace = "TestProject.Client")
    {
        var projectContent = File.ReadAllText(ProjectPath).Replace("</Project>", $"""
              <ItemGroup>
                <ProjectReference Include="..\Client\TestProject.Client.csproj" />
                <PackageReference Include="Microsoft.AspNetCore.Components.WebAssembly.Server" Version="{aspNetCoreVersion}" />
              </ItemGroup>
            </Project>
            """);
        File.WriteAllText(ProjectPath, projectContent);
        File.WriteAllText(Path.Combine(ProjectDirectory, "Program.cs"), $"""
            using TestProject.Components;

            var builder = WebApplication.CreateBuilder(args);
            builder.Services.AddRazorComponents()
                {(usesInteractiveServer ? ".AddInteractiveServerComponents()" : "")}
                .AddInteractiveWebAssemblyComponents();

            var app = builder.Build();
            app.MapRazorComponents<App>()
                {(usesInteractiveServer ? ".AddInteractiveServerRenderMode()" : "")}
                .AddInteractiveWebAssemblyRenderMode();
            app.Run();
            """);
        ScaffoldCliHelper.SetupBlazorProjectStructure(ProjectDirectory);
        File.AppendAllText(Path.Combine(ProjectDirectory, "Components", "_Imports.razor"), $"@using {clientNamespace}\n");

        Directory.CreateDirectory(ClientDirectory);
        File.WriteAllText(ClientProjectPath, $"""
            <Project Sdk="Microsoft.NET.Sdk.BlazorWebAssembly">
              <PropertyGroup>
                <TargetFramework>{_targetFramework}</TargetFramework>
                <RootNamespace>{clientNamespace}</RootNamespace>
                <ImplicitUsings>enable</ImplicitUsings>
                <Nullable>enable</Nullable>
              </PropertyGroup>
              <ItemGroup>
                <PackageReference Include="Microsoft.AspNetCore.Components.WebAssembly" Version="{aspNetCoreVersion}" />
              </ItemGroup>
            </Project>
            """);
        File.WriteAllText(Path.Combine(ClientDirectory, "Program.cs"), """
            using Microsoft.AspNetCore.Components.WebAssembly.Hosting;

            var builder = WebAssemblyHostBuilder.CreateDefault(args);
            await builder.Build().RunAsync();
            """);
        File.WriteAllText(Path.Combine(ClientDirectory, "_Imports.razor"),
            ScaffoldCliHelper.GetBlazorImportsRazor() + $"@using {clientNamespace}\n");
    }

    public void Dispose()
    {
        Directory.Delete(DirectoryPath, recursive: true);
    }
}
