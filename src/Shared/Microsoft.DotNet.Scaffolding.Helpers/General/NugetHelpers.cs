/*// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using NuGet.Protocol;
using NuGet.Versioning;

namespace Microsoft.DotNet.Scaffolding.Helpers.General;

public static class NugetHelpers
{
    public static async Task Helper()
    { 
        string packageName = "YourPackageName";

        // Get the latest stable version
        NuGetVersion latestStableVersion = await GetLatestVersion(packageName, includePrerelease: false);

        // Get the latest prerelease version
        NuGetVersion latestPrereleaseVersion = await GetLatestVersion(packageName, includePrerelease: true);

        Console.WriteLine($"Latest stable version: {latestStableVersion}");
        Console.WriteLine($"Latest prerelease version: {latestPrereleaseVersion}");
    }

    static async Task<NuGetVersion> GetLatestVersion(string packageName, bool includePrerelease)
    {
        SourceRepository repository = Repository.Factory.GetCoreV3("https://api.nuget.org/v3/index.json");
        PackageMetadataResource resource = await repository.GetResourceAsync<PackageMetadataResource>();

        IReadOnlyList<IPackageSearchMetadata> metadata = await resource.GetMetadataAsync(packageName, includePrerelease, includeUnlisted: false);

        NuGetVersion latestVersion = metadata
            .Select(x => x.Identity.Version)
            .OrderByDescending(x => x, VersionComparer.Default)
            .FirstOrDefault();

        return latestVersion;
    }
}
*/
