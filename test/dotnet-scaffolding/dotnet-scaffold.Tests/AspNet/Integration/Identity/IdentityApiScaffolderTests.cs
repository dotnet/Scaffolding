// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Microsoft.DotNet.Tools.Scaffold.AspNet.ScaffoldSteps;
using Microsoft.DotNet.Tools.Scaffold.Tests.Helpers;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Microsoft.DotNet.Tools.Scaffold.Tests.AspNet.Integration.Identity;

public class IdentityApiScaffolderTests
{
    [Theory]
    [InlineData("net10.0")]
    [InlineData("net11.0")]
    public void TemplateAndOutputBaselineMatchPinnedReleaseImplementation(string framework)
    {
        var baseline = Path.Combine(ScaffoldCliHelper.GetRepoRoot(), "test", "dotnet-scaffolding", "baselines",
            "IdentityApi", framework, "EmptyWebApp", "IdentityApi", "IdentityApiEndpoints.cs");
        foreach (var path in new[] { GetEndpointTemplatePath(), baseline })
        {
            var source = File.ReadAllText(path).ReplaceLineEndings("\n")
                .Replace("// Adapted from dotnet/aspnetcore release/11.0 at ec5a3b43a7186a2a14d88695e7b8e435797f6a0f\n", "")
                .Replace("// (identical source in release/10.0 at 55d77e1e0349aa2a079a516f6ecb7c79b56bf85c).\n", "")
                .Replace("// The class and mapping method are renamed to coexist with the framework API.\n", "")
                .Replace("ScaffoldedIdentityApiEndpointRouteBuilderExtensions", "IdentityApiEndpointRouteBuilderExtensions")
                .Replace("MapScaffoldedIdentityApi", "MapIdentityApi");

            Assert.Equal("581C0463F674395DE74C6BF992E9686DD8FBEC49CF59D8ADF3A8407756CC3350",
                Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(source))));
        }
    }

    [Fact]
    public async Task CopiesEndpointSourcesAndPreservesCustomizationsOnRerun()
    {
        var directory = Path.Combine(Path.GetTempPath(), nameof(IdentityApiScaffolderTests), Guid.NewGuid().ToString());
        Directory.CreateDirectory(directory);
        try
        {
            var projectPath = Path.Combine(directory, "TestApp.csproj");
            File.WriteAllText(projectPath, "<Project Sdk=\"Microsoft.NET.Sdk.Web\"><PropertyGroup><TargetFramework>net11.0</TargetFramework></PropertyGroup></Project>");
            var template = GetEndpointTemplatePath();
            var step = new AddIdentityApiFilesStep(NullLogger<AddIdentityApiFilesStep>.Instance)
            {
                ProjectPath = projectPath,
                TemplatePaths = [template, Path.Combine(Path.GetDirectoryName(template)!, "LICENSE.txt")]
            };

            Assert.True(await step.ExecuteAsync(null!));
            var outputPath = Path.Combine(directory, "IdentityApi", "IdentityApiEndpoints.cs");
            Assert.True(File.Exists(outputPath));
            var original = File.ReadAllText(outputPath);
            Assert.Contains("MapScaffoldedIdentityApi", original);
            Assert.Contains("forgotPassword", original, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("Permission is hereby granted", File.ReadAllText(Path.Combine(directory, "IdentityApi", "LICENSE.txt")));

            File.WriteAllText(outputPath, "// customized");
            Assert.True(await step.ExecuteAsync(null!));
            Assert.Equal("// customized", File.ReadAllText(outputPath));

            step.Overwrite = true;
            Assert.True(await step.ExecuteAsync(null!));
            Assert.Equal(original, File.ReadAllText(outputPath));
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task MissingSourceOrLicenseFailsWithoutWritingFiles(bool missingLicense)
    {
        var directory = Path.Combine(Path.GetTempPath(), nameof(IdentityApiScaffolderTests), Guid.NewGuid().ToString());
        Directory.CreateDirectory(directory);
        try
        {
            var template = GetEndpointTemplatePath();
            var step = new AddIdentityApiFilesStep(NullLogger<AddIdentityApiFilesStep>.Instance)
            {
                ProjectPath = Path.Combine(directory, "TestApp.csproj"),
                TemplatePaths = [missingLicense ? template : Path.Combine(Path.GetDirectoryName(template)!, "LICENSE.txt")]
            };
            Assert.False(await step.ExecuteAsync(null!));
            Assert.False(Directory.Exists(Path.Combine(directory, "IdentityApi")));
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    private static string GetEndpointTemplatePath() =>
        Path.Combine(ScaffoldCliHelper.GetRepoRoot(),
            "src", "dotnet-scaffolding", "dotnet-scaffold", "AspNet", "Templates", "net10.0", "IdentityApi", "IdentityApiEndpoints.cs.txt");
}
