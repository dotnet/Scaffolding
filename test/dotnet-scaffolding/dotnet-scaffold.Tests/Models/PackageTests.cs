// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Threading.Tasks;
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
    private readonly NuGetVersionService _nugetVersionService;

    public PackageTests()
    {
        _mockEnvironmentService = new Mock<IEnvironmentService>();
        _mockEnvironmentService.Setup(e => e.CurrentDirectory).Returns(System.IO.Directory.GetCurrentDirectory());
        _nugetVersionService = new NuGetVersionService(_mockEnvironmentService.Object);
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

    [Fact]
    public async Task WithResolvedVersionAsync_Net8Framework_ResolvesVersion()
    {
        // Arrange
        Package package = new Package("Microsoft.Extensions.Logging", IsVersionRequired: true);

        // Act
        Package result = await package.WithResolvedVersionAsync(TargetFramework.Net8, _nugetVersionService);

        // Assert
        Assert.NotNull(result.PackageVersion);
        Assert.NotEqual(package, result);
    }

    [Fact]
    public async Task WithResolvedVersionAsync_Net9Framework_ResolvesVersion()
    {
        // Arrange
        Package package = new Package("Microsoft.Extensions.Logging", IsVersionRequired: true);

        // Act
        Package result = await package.WithResolvedVersionAsync(TargetFramework.Net9, _nugetVersionService);

        // Assert
        Assert.NotNull(result.PackageVersion);
        Assert.NotEqual(package, result);
    }

    [Fact]
    public async Task WithResolvedVersionAsync_Net10Framework_ResolvesVersion()
    {
        // Arrange
        Package package = new Package("Microsoft.Extensions.Logging", IsVersionRequired: true);

        // Act
        Package result = await package.WithResolvedVersionAsync(TargetFramework.Net10, _nugetVersionService);

        // Assert
        Assert.NotNull(result.PackageVersion);
        Assert.NotEqual(package, result);
    }

    [Fact]
    public async Task WithResolvedVersionAsync_Net11Framework_ResolvesVersion()
    {
        // Arrange
        Package package = new Package("Microsoft.Extensions.Logging", IsVersionRequired: true);

        // Act
        Package result = await package.WithResolvedVersionAsync(TargetFramework.Net11, _nugetVersionService);

        // Assert
        Assert.NotNull(result.PackageVersion);
        Assert.NotEqual(package, result);
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
}
