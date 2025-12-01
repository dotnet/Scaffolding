// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using NuGet.Protocol.Core.Types;
using NuGet.Versioning;
using NuGet.Configuration;

namespace Microsoft.DotNet.Scaffolding.Core.Model
{
    public static class NuGetVersionHelper
    {
        /// <summary>
        /// Gets the latest major version 8 of a NuGet package (e.g., for .NET 8).
        /// </summary>
        public static async Task<NuGetVersion?> GetLatestNet8VersionAsync(string packageId)
        {
            IEnumerable<NuGetVersion> versions = await GetVersionsForPackageAsync(packageId);

            // Filter for .NET 8 compatible versions (major version 8)
            var net8Versions = versions.Where(v => v.Major == 8).OrderByDescending(v => v);
            return net8Versions.FirstOrDefault();
        }

        /// <summary>
        /// Gets the latest major version 9 of a NuGet package (e.g., for .NET 9).
        /// </summary>
        public static async Task<NuGetVersion?> GetLatestNet9VersionAsync(string packageId)
        {
            IEnumerable<NuGetVersion> versions = await GetVersionsForPackageAsync(packageId);

            // Filter for .NET 9 compatible versions (major version 9)
            var net9Versions = versions.Where(v => v.Major == 9).OrderByDescending(v => v);
            return net9Versions.FirstOrDefault();
        }

        /// <summary>
        /// Gets the latest major version 10 of a NuGet package (e.g., for .NET 10).
        /// </summary>
        public static async Task<NuGetVersion?> GetLatestNet10VersionAsync(string packageId)
        {
            IEnumerable<NuGetVersion> versions = await GetVersionsForPackageAsync(packageId);

            // Filter for .NET 10 compatible versions (major version 10)
            var net10Versions = versions.Where(v => v.Major == 10).OrderByDescending(v => v);
            return net10Versions.FirstOrDefault();
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
}
