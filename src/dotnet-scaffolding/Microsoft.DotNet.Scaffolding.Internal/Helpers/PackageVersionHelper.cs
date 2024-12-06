// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using System.Text.Json;

namespace Microsoft.DotNet.Scaffolding.Internal.Helpers;

internal static class PackageVersionHelper
{
    public static async Task<string?> GetPackageVersionFromTfmAsync(string packageName, string shortTfm)
    {
        // Base URL for the NuGet API
        // might change in the future but unit tests should catch that
        var url = $"https://api.nuget.org/v3-flatcontainer/{packageName.ToLowerInvariant()}/index.json";
        try
        {
            var response = await new HttpClient().GetStringAsync(url);
            var data = JsonDocument.Parse(response);
            if (data is not null)
            {
                var tfmMajorMinor = ParseFrameworkVersion(shortTfm)?.ToString();
                if (string.IsNullOrEmpty(tfmMajorMinor))
                {
                    return null;
                }

                // Extract versions pertaining the particular short tfm.
                // Removing '-' takes care of removing all prerelease versions
                var versions = data.RootElement
                    .GetProperty("versions")
                    .EnumerateArray()
                    .Select(x => x.GetString())
                    .Where(x =>
                        !string.IsNullOrEmpty(x) &&
                        x.StartsWith(tfmMajorMinor) &&
                        !x.Contains('-'))
                    .ToList();

                //the last one in the list is the highest version.
                return versions.LastOrDefault();
            }
        }
        catch (Exception)
        {
            return null;
        }

        return null;
    }

    /// <summary>
    /// Method to parse version from a TFM string.
    /// Given a short tfm like "net5.0", it will return a Version object with just major and minor.
    /// eg, 5.0
    /// only works with "netX.0" variants
    /// </summary>
    internal static Version? ParseFrameworkVersion(string? tfm)
    {
        if (string.IsNullOrEmpty(tfm) ||
            !System.Text.RegularExpressions.Regex.IsMatch(tfm, @"^net\d+\.\d+$"))
        {
            return null;
        }

        // Remove "net" prefix to parse the version
        string versionPart = tfm.Replace("net", "");

        // Parse to Version; assume "0.0" for invalid formats
        return Version.TryParse(versionPart, out var version) ? version : new Version(0, 0);
    }
}
