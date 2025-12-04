// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using Microsoft.DotNet.Scaffolding.Internal.Services;
using NuGet.Protocol.Core.Types;
using NuGet.Versioning;
using NuGet.Configuration;
using NuGet.Protocol;

namespace Microsoft.DotNet.Scaffolding.Core.Model;

internal class NuGetVersionService
{
    private readonly IEnvironmentService _environmentService;

    public NuGetVersionService(IEnvironmentService environmentService) => _environmentService = environmentService;

    /// <summary>
    /// Gets the latest version of a NuGet package for the .NET major version specified (e.g., 8 for .NET 8).
    /// </summary>
    public async Task<NuGetVersion?> GetLatestPackageForNetVersionAsync(string packageId, int MajorVersion)
    {
        IEnumerable<NuGetVersion> versions = await GetVersionsForPackageAsync(packageId);

        // Filter for the specified .NET major version
        var compatibleVersions = versions.Where(v => v.Major == MajorVersion).OrderByDescending(v => v);
        return compatibleVersions.FirstOrDefault();
    }

    private async Task<IEnumerable<NuGetVersion>> GetVersionsForPackageAsync(string packageId)
    {
        // Load NuGet settings from the current directory
        string settingsPath = _environmentService.CurrentDirectory;
        ISettings settings = Settings.LoadDefaultSettings(settingsPath);

        // Load package sources and filter enabled ones
        PackageSourceProvider sourceProvider = new(settings);
        IEnumerable<PackageSource> packageSources = sourceProvider.LoadPackageSources().Where(s => s.IsEnabled);

        // Check if package source mapping is enabled
        PackageSourceMapping? sourceMapping = PackageSourceMapping.GetPackageSourceMapping(settings);
        if (sourceMapping?.IsEnabled == true)
        {
            IReadOnlyList<string> configuredSources = sourceMapping.GetConfiguredPackageSources(packageId);
            if (configuredSources.Any())
            {
                packageSources = packageSources.Where(s => configuredSources.Contains(s.Name));
            }
        }

        // Use CachingSourceProvider to cache SourceRepository instances
        CachingSourceProvider cachingProvider = new(sourceProvider);
        List<SourceRepository> repositories = [.. packageSources
            .Select(source => cachingProvider.CreateRepository(source))];

        if (repositories.Count == 0)
        {
            return [];
        }

        // Query all sources in parallel to get package metadata
        SourceCacheContext cache = new();
        var allVersionsTasks = repositories.Select(async repo =>
        {
            try
            {
                PackageMetadataResource? metadataResource = await repo.GetResourceAsync<PackageMetadataResource>();
                if (metadataResource == null)
                {
                    return [];
                }

                IEnumerable<IPackageSearchMetadata> metadata = await metadataResource.GetMetadataAsync(
                    packageId,
                    includePrerelease: true,
                    includeUnlisted: false,
                    cache,
                    NuGet.Common.NullLogger.Instance,
                    CancellationToken.None);

                return metadata;
            }
            catch
            {
                // If a source fails, continue with other sources
                return [];
            }
        });

        IEnumerable<IPackageSearchMetadata>[] allMetadataResults = await Task.WhenAll(allVersionsTasks);

        // Merge and deduplicate versions from all sources
        HashSet<NuGetVersion> allVersions = [];
        foreach (var metadataList in allMetadataResults)
        {
            foreach (var metadata in metadataList)
            {
                if (metadata.Identity?.Version != null)
                {
                    allVersions.Add(metadata.Identity.Version);
                }
            }
        }

        return allVersions.OrderByDescending(v => v);
    }
}
