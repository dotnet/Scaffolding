// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Threading.Tasks;
using System;
using System.IO;
using System.Collections.Generic;
using Microsoft.DotNet.Scaffolding.Core.Model;
using Microsoft.DotNet.Scaffolding.Internal.Services;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using NuGet.Versioning;
using Xunit;

namespace Microsoft.DotNet.Tools.Scaffold.Tests.Models;

public class PackageTests
{
    private readonly Mock<IEnvironmentService> _mockEnvironmentService;
    private readonly FixtureNuGetVersionService _nugetVersionService;

    public PackageTests()
    {
        _mockEnvironmentService = new Mock<IEnvironmentService>();
        _mockEnvironmentService.Setup(e => e.CurrentDirectory).Returns(System.IO.Directory.GetCurrentDirectory());
        _nugetVersionService = new FixtureNuGetVersionService(_mockEnvironmentService.Object);
    }

    [Fact]
    public async Task WithResolvedVersionAsync_PackageAlreadyHasVersion_ReturnsSamePackage()
    {
        // Arrange
        Package package = new Package("TestPackage", IsVersionRequired: true)
        {
            PackageVersion = "1.0.0"
        };

        // Act
        Package result = await package.WithResolvedVersionAsync(TargetFramework.Net8, _nugetVersionService);

        // Assert
        Assert.Same(package, result);
        Assert.Equal("1.0.0", result.PackageVersion);
    }

    [Theory]
    [InlineData(TargetFramework.Net8, "8.0.2")]
    [InlineData(TargetFramework.Net9, "9.0.1")]
    [InlineData(TargetFramework.Net10, "10.0.1")]
    [InlineData(TargetFramework.Net11, "11.0.0-rc.1")]
    public async Task WithResolvedVersionAsync_ResolvesHighestVersionForTargetFramework(TargetFramework framework, string expected)
    {
        // Arrange
        Package package = new Package("Microsoft.Extensions.Logging", IsVersionRequired: true);

        // Act
        Package result = await package.WithResolvedVersionAsync(framework, _nugetVersionService);

        // Assert
        Assert.Equal(expected, result.PackageVersion);
        Assert.NotEqual(package, result);
    }

    [Fact]
    public async Task WithResolvedVersionAsync_Net10EfPackage_UsesAspNetReferenceVersion()
    {
        // Arrange
        string tempProjectPath = Path.Combine(Path.GetTempPath(), $"{nameof(PackageTests)}-{Guid.NewGuid():N}.csproj");
        await File.WriteAllTextAsync(
            tempProjectPath,
            """
            <Project Sdk="Microsoft.NET.Sdk.Web">
              <PropertyGroup>
                <TargetFramework>net10.0</TargetFramework>
              </PropertyGroup>
              <ItemGroup>
                <PackageReference Include="Microsoft.AspNetCore.Diagnostics.EntityFrameworkCore" Version="10.0.0-preview.5.25277.114" />
              </ItemGroup>
            </Project>
            """);

        Package package = new Package("Microsoft.EntityFrameworkCore.Tools", IsVersionRequired: true);

        try
        {
            // Act
            Package result = await package.WithResolvedVersionAsync(TargetFramework.Net10, _nugetVersionService, tempProjectPath);

            // Assert
            Assert.Equal("10.0.0-preview.5.25277.114", result.PackageVersion);
            Assert.NotEqual(package, result);
        }
        finally
        {
            File.Delete(tempProjectPath);
        }
    }

    [Fact]
    public async Task WithResolvedVersionAsync_UseLatestVersion_ResolvesLatestStableSqlitePclRawBundle()
    {
        // SQLitePCLRaw is not versioned in lockstep with .NET.
        Package package = new Package("SQLitePCLRaw.bundle_e_sqlite3", IsVersionRequired: true)
        {
            UseLatestVersion = true
        };

        // Act
        Package result = await package.WithResolvedVersionAsync(TargetFramework.Net10, _nugetVersionService);

        // Assert
        Assert.NotNull(result.PackageVersion);
        NuGetVersion resolved = NuGetVersion.Parse(result.PackageVersion!);
        Assert.False(resolved.IsPrerelease);
        Assert.Equal(NuGetVersion.Parse("3.0.0"), resolved);
    }

    [Fact]
    public async Task WithResolvedVersionAsync_UsesTargetProjectDirectory()
    {
        // Arrange
        Package package = new Package("Microsoft.Extensions.Logging", IsVersionRequired: true);

        // Act
        string projectPath = Path.Combine(Path.GetTempPath(), "fixture", "App.csproj");
        Package result = await package.WithResolvedVersionAsync(TargetFramework.Net11, _nugetVersionService, projectPath);

        // Assert
        Assert.Equal("11.0.0-rc.1", result.PackageVersion);
        Assert.Equal(Path.GetDirectoryName(projectPath), _nugetVersionService.ProjectDirectory);
    }

    [Fact]
    public async Task WithResolvedVersionAsync_NullTargetFramework_ReturnsOriginalPackage()
    {
        // Arrange
        Package package = new Package("TestPackage", IsVersionRequired: true);

        // Act
        Package result = await package.WithResolvedVersionAsync(null, _nugetVersionService, NullLogger.Instance);

        // Assert
        Assert.Same(package, result);
        Assert.Null(result.PackageVersion);
    }

    [Fact]
    public async Task WithResolvedVersionAsync_PackageDoesNotRequireVersion_ReturnsOriginalPackage()
    {
        // Arrange
        Package package = new Package("TestPackage", IsVersionRequired: false);

        // Act
        Package result = await package.WithResolvedVersionAsync(TargetFramework.Net8, _nugetVersionService);

        // Assert
        Assert.Same(package, result);
        Assert.Null(result.PackageVersion);
    }

    [Fact]
    public async Task WithResolvedVersionAsync_InvalidPackageName_ReturnsOriginalPackage()
    {
        // Arrange
        Package package = new Package("NonExistentPackage12345678", IsVersionRequired: true);

        // Act
        Package result = await package.WithResolvedVersionAsync(TargetFramework.Net8, _nugetVersionService);

        // Assert
        Assert.Same(package, result);
        Assert.Null(result.PackageVersion);
    }

    [Fact]
    public async Task WithResolvedVersionAsync_PreservesPackageName()
    {
        // Arrange
        string packageName = "TestPackage";
        Package package = new Package(packageName, IsVersionRequired: false);

        // Act
        Package result = await package.WithResolvedVersionAsync(TargetFramework.Net8, _nugetVersionService);

        // Assert
        Assert.Equal(packageName, result.Name);
    }

    [Fact]
    public async Task WithResolvedVersionAsync_PreservesIsVersionRequired()
    {
        // Arrange
        Package package = new Package("TestPackage", IsVersionRequired: true)
        {
            PackageVersion = "1.0.0"
        };

        // Act
        Package result = await package.WithResolvedVersionAsync(TargetFramework.Net8, _nugetVersionService);

        // Assert
        Assert.True(result.IsVersionRequired);
    }

    [Fact]
    public void Package_Record_Equality()
    {
        // Arrange
        Package package1 = new Package("TestPackage", IsVersionRequired: true)
        {
            PackageVersion = "1.0.0"
        };
        Package package2 = new Package("TestPackage", IsVersionRequired: true)
        {
            PackageVersion = "1.0.0"
        };

        // Assert
        Assert.Equal(package1, package2);
    }

    [Fact]
    public void Package_Record_WithModification()
    {
        // Arrange
        Package package = new Package("TestPackage", IsVersionRequired: true);

        // Act
        Package modifiedPackage = package with { PackageVersion = "2.0.0" };

        // Assert
        Assert.Equal("TestPackage", modifiedPackage.Name);
        Assert.True(modifiedPackage.IsVersionRequired);
        Assert.Equal("2.0.0", modifiedPackage.PackageVersion);
        Assert.NotEqual(package, modifiedPackage);
    }

    private sealed class FixtureNuGetVersionService(IEnvironmentService environmentService)
        : NuGetVersionService(environmentService)
    {
        public string? ProjectDirectory { get; private set; }

        protected override Task<IEnumerable<NuGetVersion>> GetVersionsForPackageAsync(string packageId, string? projectDirectory = null)
        {
            ProjectDirectory = projectDirectory;
            string[] versions = packageId switch
            {
                "Microsoft.Extensions.Logging" => ["10.0.1", "8.0.1", "11.0.0-rc.1", "9.0.1", "8.0.2", "11.0.0-preview.1"],
                "SQLitePCLRaw.bundle_e_sqlite3" => ["2.1.11", "3.0.0", "4.0.0-preview.1"],
                _ => []
            };
            IEnumerable<NuGetVersion> result = Array.ConvertAll(versions, NuGetVersion.Parse);
            return Task.FromResult(result);
        }
    }
}
