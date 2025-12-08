// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using NuGet.Versioning;

namespace Microsoft.DotNet.Scaffolding.Core.Model;

/// <summary>
/// Represents a NuGet package with a specified name and optional version.
/// </summary>
internal sealed record Package(string Name, bool IsVersionRequired = false)
{
    /// <summary>
    /// Gets or sets the package version.
    /// </summary>
    public string? PackageVersion { get; init; }
}

internal static class PackageExtensions
{
    /// <summary>
    /// Returns a copy of the specified package with its version property resolved for the given target framework, if
    /// the version is not already set.
    /// </summary>
    /// <remarks>If the package already has a version specified, this method returns the original package
    /// instance. If the version cannot be resolved for the given target framework, the original package is returned
    /// unchanged.</remarks>
    /// <param name="package">The package instance for which to resolve the version. Must not be null.</param>
    /// <param name="targetFramework">The target framework identifier used to determine the appropriate package version. For example, "net6.0".</param>
    /// <param name="nugetVersionHelper">The NuGet version helper to use for version resolution.</param>
    /// <returns>A package instance with the version property set to the resolved version for the specified target framework, or
    /// the original package if the version is already set or cannot be resolved.</returns>
    public static async Task<Package> WithResolvedVersionAsync(this Package package, string targetFramework, NuGetVersionService nugetVersionHelper)
    {
        if (package.PackageVersion is not null)
        {
            return package;
        }
        NuGetVersion? resolvedVersion = await package.GetVersionForTargetFrameworkAsync(targetFramework, nugetVersionHelper);
        if (resolvedVersion is null)
        {
            return package;
        }
        return package with { PackageVersion = resolvedVersion.ToNormalizedString() };
    }

    /// <summary>
    /// Asynchronously retrieves the NuGet package version that corresponds to the specified target framework.
    /// </summary>
    /// <remarks>If the package does not require a version, the returned task will complete with <see
    /// langword="null"/>. For supported target frameworks, the latest available version is retrieved
    /// asynchronously.</remarks>
    /// <param name="package">The package for which to obtain the version information. Must not be null.</param>
    /// <param name="targetFramework">The target framework identifier (for example, "net8.0", "net9.0", or "net10.0") for which the package version is
    /// requested. Case-insensitive.</param>
    /// <param name="nugetVersionHelper">The NuGet version helper to use for version resolution.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the corresponding NuGet package
    /// version if available; otherwise, <see langword="null"/> if the package does not require a version.</returns>
    /// <exception cref="NotSupportedException">Thrown if <paramref name="targetFramework"/> is not one of the supported frameworks ("net8.0", "net9.0", or
    /// "net10.0").</exception>
    private static Task<NuGetVersion?> GetVersionForTargetFrameworkAsync(this Package package, string targetFramework, NuGetVersionService nugetVersionHelper)
    {
        if (!package.IsVersionRequired)
        {
            return Task.FromResult<NuGetVersion?>(null);
        }

        if (targetFramework.Equals(TargetFrameworkConstants.Net8, StringComparison.OrdinalIgnoreCase))
        {
            return nugetVersionHelper.GetLatestPackageForNetVersionAsync(package.Name, 8);
        }
        else if (targetFramework.Equals(TargetFrameworkConstants.Net9, StringComparison.OrdinalIgnoreCase))
        {
            return nugetVersionHelper.GetLatestPackageForNetVersionAsync(package.Name, 9);
        }
        else
        {
            throw new NotSupportedException($"Target framework '{targetFramework}' is not supported.");
        }
    }
}
