// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using Microsoft.DotNet.Scaffolding.Internal.Services;
using NuGet.Protocol.Core.Types;
using NuGet.Versioning;
using NuGet.Configuration;
using NuGet.Protocol;
using NuGet.Credentials;

namespace Microsoft.DotNet.Scaffolding.Core.Model;

internal class NuGetVersionService
{
    private readonly IEnvironmentService _environmentService;
    private readonly ISettings _settings;
    private readonly PackageSourceProvider _sourceProvider;
    private readonly CachingSourceProvider _cachingProvider;
    private readonly PackageSourceMapping? _sourceMapping;

    public NuGetVersionService(IEnvironmentService environmentService)
    {
        _environmentService = environmentService;

        DefaultCredentialServiceUtility.SetupDefaultCredentialService(
            NuGet.Common.NullLogger.Instance,
            nonInteractive: false);

        string settingsPath = _environmentService.CurrentDirectory;
        _settings = Settings.LoadDefaultSettings(settingsPath);
        _sourceProvider = new PackageSourceProvider(_settings);
        _cachingProvider = new CachingSourceProvider(_sourceProvider);
        _sourceMapping = PackageSourceMapping.GetPackageSourceMapping(_settings);
    }

    /// <summary>
    /// Gets the latest NuGet package version compatible with the specified .NET major version.
    /// </summary>
    /// <param name="packageId">the ID of the NuGet package</param>
    /// <param name="MajorVersion">the .NET major version</param>
    /// <param name="projectDirectory">
    /// Optional directory of the project being scaffolded. When provided, NuGet settings are loaded
    /// from this directory so that the same package sources used by <c>dotnet add package</c> are
    /// queried, rather than the repo-root NuGet.config (which may have <c>&lt;clear /&gt;</c> and
    /// no nuget.org feed).
    /// </param>
    /// <returns></returns>
    public async Task<NuGetVersion?> GetLatestPackageForNetVersionAsync(string packageId, int MajorVersion, string? projectDirectory = null)
    {
        IEnumerable<NuGetVersion> versions = await GetVersionsForPackageAsync(packageId, projectDirectory);

        // Filter for the specified .NET major version
        var compatibleVersions = versions.Where(v => v.Major == MajorVersion).OrderByDescending(v => v);
        return compatibleVersions.FirstOrDefault();
    }

    private async Task<IEnumerable<NuGetVersion>> GetVersionsForPackageAsync(string packageId, string? projectDirectory = null)
    {
        // When a project directory is provided, load settings from there so that the package
        // sources match the context used by 'dotnet add package' (e.g. a temp project directory
        // that is outside the repo and therefore does not inherit the repo-root NuGet.config with
        // <clear /> that strips away nuget.org).
        ISettings effectiveSettings = projectDirectory is not null
            ? Settings.LoadDefaultSettings(projectDirectory)
            : _settings;
        PackageSourceProvider effectiveSourceProvider = projectDirectory is not null
            ? new PackageSourceProvider(effectiveSettings)
            : _sourceProvider;
        CachingSourceProvider effectiveCachingProvider = projectDirectory is not null
            ? new CachingSourceProvider(effectiveSourceProvider)
            : _cachingProvider;
        PackageSourceMapping? effectiveSourceMapping = projectDirectory is not null
            ? PackageSourceMapping.GetPackageSourceMapping(effectiveSettings)
            : _sourceMapping;

        // Load package sources and filter enabled ones
        IEnumerable<PackageSource> packageSources = effectiveSourceProvider.LoadPackageSources().Where(s => s.IsEnabled);

        // Check if package source mapping is enabled
        if (effectiveSourceMapping?.IsEnabled == true)
        {
            IReadOnlyList<string> configuredSources = effectiveSourceMapping.GetConfiguredPackageSources(packageId);
            if (configuredSources.Any())
            {
                packageSources = packageSources.Where(s => configuredSources.Contains(s.Name));
            }
        }

        List<SourceRepository> repositories = [.. packageSources
            .Select(source => effectiveCachingProvider.CreateRepository(source))];

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
