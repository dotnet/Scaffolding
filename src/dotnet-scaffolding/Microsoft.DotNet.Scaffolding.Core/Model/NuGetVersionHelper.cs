// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using NuGet.Protocol.Core.Types;
using NuGet.Versioning;
using NuGet.Configuration;

namespace Microsoft.DotNet.Scaffolding.Core.Model;

public static class NuGetVersionHelper
{
    /// <summary>
    /// Gets the latest version of a NuGet package for the .NET major version specified (e.g., 8 for .NET 8).
    /// </summary>
    public static async Task<NuGetVersion?> GetLatestPackageForNetVersionAsync(string packageId, int MajorVersion)
    {
        IEnumerable<NuGetVersion> versions = await GetVersionsForPackageAsync(packageId);

        // Filter for .NET 8 compatible versions (major version 8)
        var net8Versions = versions.Where(v => v.Major == MajorVersion).OrderByDescending(v => v);
        return net8Versions.FirstOrDefault();
    }

    private static async Task<IEnumerable<NuGetVersion>> GetVersionsForPackageAsync(string packageId)
    {
        IEnumerable<Lazy<INuGetResourceProvider>> providers = Repository.Provider.GetCoreV3();
        PackageSource source = new("https://api.nuget.org/v3/index.json");
        SourceRepository repo = new(source, providers);
        FindPackageByIdResource resource = await repo.GetResourceAsync<FindPackageByIdResource>();
        IEnumerable<NuGetVersion> versions = await resource.GetAllVersionsAsync(packageId, new SourceCacheContext(), NuGet.Common.NullLogger.Instance, CancellationToken.None);
        return versions;
    }
}
