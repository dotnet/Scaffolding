// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.Logging;
using NuGet.Versioning;
using System.Xml.Linq;

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
    private static readonly StringComparer PackageNameComparer = StringComparer.OrdinalIgnoreCase;
    private static readonly string[] AspNetEfVersionAnchorPackages =
    [
        PackageNameConstants.AspNetCorePackages.IdentityEntityFrameworkCorePackageName,
        PackageNameConstants.AspNetCorePackages.DiagnosticsEntityFrameworkCorePackageName
    ];

    public static Task<Package> WithResolvedVersionAsync(this Package package, TargetFramework? targetFramework, NuGetVersionService nugetVersionHelper, ILogger? logger)
    {
        return package.WithResolvedVersionAsync(targetFramework, nugetVersionHelper, projectPath: null, logger: logger);
    }

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
    /// <param name="projectPath">The path to the target project file. Used for project-specific version alignment.</param>
    /// <param name="logger">The logger to use for logging messages.</param>
    /// <returns>A package instance with the version property set to the resolved version for the specified target framework, or
    /// the original package if the version is already set or cannot be resolved.</returns>
    public static async Task<Package> WithResolvedVersionAsync(this Package package, TargetFramework? targetFramework, NuGetVersionService nugetVersionHelper, string? projectPath = null, ILogger? logger = null)
    {
        if (package.PackageVersion is not null)
        {
            return package;
        }
        NuGetVersion? resolvedVersion = await package.GetVersionForTargetFrameworkAsync(targetFramework, nugetVersionHelper, projectPath, logger);
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
    /// <param name="targetFramework">The target framework identifier (for example, "net8.0", "net9.0", "net10.0" or "net11.0") for which the package version is
    /// requested. Case-insensitive.</param>
    /// <param name="nugetVersionHelper">The NuGet version helper to use for version resolution.</param>
    /// <param name="projectPath">The path to the target project file. Used for project-specific version alignment.</param>
    /// <param name="logger">The logger to use for logging messages.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the corresponding NuGet package
    /// version if available; otherwise, <see langword="null"/> if the package does not require a version or target framework is not supported.</returns>
    private static Task<NuGetVersion?> GetVersionForTargetFrameworkAsync(this Package package, TargetFramework? targetFramework, NuGetVersionService nugetVersionHelper, string? projectPath = null, ILogger? logger = null)
    {
        if (!package.IsVersionRequired)
        {
            return Task.FromResult<NuGetVersion?>(null);
        }

        if (targetFramework is null)
        {
            logger?.LogError("Project contains a Target Framework that is not supported. Supported Target Frameworks are .NET8, .NET9, .NET10, .NET11. Installing latest stable version of '{PackageName}'. Consider upgrading your Target Framework to install a compatible package version.", package.Name);
            return Task.FromResult<NuGetVersion?>(null);
        }

        if (TryResolveAspNetAlignedEfVersion(package.Name, projectPath, out NuGetVersion? alignedVersion))
        {
            return Task.FromResult<NuGetVersion?>(alignedVersion);
        }

        if (targetFramework is TargetFramework.Net8)
        {
            return nugetVersionHelper.GetLatestPackageForNetVersionAsync(package.Name, 8);
        }
        else if (targetFramework is TargetFramework.Net9)
        {
            return nugetVersionHelper.GetLatestPackageForNetVersionAsync(package.Name, 9);
        }
        else if (targetFramework is TargetFramework.Net10)
        {
            return nugetVersionHelper.GetLatestPackageForNetVersionAsync(package.Name, 10);
        }
        else if (targetFramework is TargetFramework.Net11)
        {
            return nugetVersionHelper.GetLatestPackageForNetVersionAsync(package.Name, 11);
        }
        else
        {
            logger?.LogError("Target Framework '{TargetFramework}' is not supported. Supported Target Frameworks are .NET8, .NET9, .NET10, .NET11. Installing latest stable version of '{PackageName}'. Consider upgrading your Target Framework to install a compatible package version.", targetFramework, package.Name);
            return Task.FromResult<NuGetVersion?>(null);
        }
    }

    private static bool TryResolveAspNetAlignedEfVersion(string packageName, string? projectPath, out NuGetVersion? version)
    {
        version = null;

        if (string.IsNullOrWhiteSpace(projectPath) ||
            !packageName.StartsWith(PackageNameConstants.AspNetCorePackages.EntityFrameworkPackageNamePrefix, StringComparison.OrdinalIgnoreCase) ||
            !File.Exists(projectPath))
        {
            return false;
        }

        try
        {
            XDocument projectDocument = XDocument.Load(projectPath);
            if (projectDocument.Root is not XElement root)
            {
                return false;
            }

            XName packageReference = root.Name.Namespace + "PackageReference";

            foreach (string anchorPackage in AspNetEfVersionAnchorPackages)
            {
                NuGetVersion? matchedVersion = projectDocument
                    .Descendants(packageReference)
                    .Where(p => PackageNameComparer.Equals((string?)p.Attribute("Include"), anchorPackage))
                    .Select(GetPackageReferenceVersion)
                    .Where(v => NuGetVersion.TryParse(v, out _))
                    .Select(v => NuGetVersion.Parse(v!))
                    .FirstOrDefault();

                if (matchedVersion is not null)
                {
                    version = matchedVersion;
                    return true;
                }
            }
        }
        catch
        {
            return false;
        }

        return false;
    }

    private static string? GetPackageReferenceVersion(XElement packageReference)
    {
        string? version = (string?)packageReference.Attribute("Version");
        if (!string.IsNullOrWhiteSpace(version))
        {
            return version;
        }

        string? versionOverride = (string?)packageReference.Attribute("VersionOverride");
        if (!string.IsNullOrWhiteSpace(versionOverride))
        {
            return versionOverride;
        }

        XName versionElementName = packageReference.Name.Namespace + "Version";
        XElement? versionElement = packageReference.Element(versionElementName);
        if (versionElement is not null && !string.IsNullOrWhiteSpace(versionElement.Value))
        {
            return versionElement.Value;
        }

        XName versionOverrideElementName = packageReference.Name.Namespace + "VersionOverride";
        XElement? versionOverrideElement = packageReference.Element(versionOverrideElementName);
        if (versionOverrideElement is not null && !string.IsNullOrWhiteSpace(versionOverrideElement.Value))
        {
            return versionOverrideElement.Value;
        }

        return null;
    }
}
