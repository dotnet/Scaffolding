// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Immutable;

namespace Microsoft.DotNet.Scaffolding.Core.Model;

internal static class TargetFrameworkConstants
{
    public const string TargetFrameworkPropertyName = "TargetFramework";

    public const string Net8 = "net8.0";
    public const string Net9 = "net9.0";
    public const string Net10 = "net10.0";
    public const string Net11 = "net11.0";
    public const string NetCoreApp = ".NETCoreApp";

    public static readonly ImmutableArray<string> SupportedTargetFrameworks = [Net8, Net9, Net10, Net11];

    public static readonly ImmutableDictionary<string, TargetFramework> TargetFrameworkMapping = new Dictionary<string, TargetFramework>(StringComparer.OrdinalIgnoreCase)
    {
        [Net8] = TargetFramework.Net8,
        [Net9] = TargetFramework.Net9,
        [Net10] = TargetFramework.Net10,
        [Net11] = TargetFramework.Net11
    }.ToImmutableDictionary(StringComparer.OrdinalIgnoreCase);
}

public enum TargetFramework
{
    Net8,
    Net9,
    Net10,
    Net11
}

internal static class TargetFrameworkExtensions
{
    /// <summary>
    /// Attempts to get the .NET major version number (for example, 9 for <see cref="TargetFramework.Net9"/>)
    /// for the specified target framework.
    /// </summary>
    /// <param name="targetFramework">The target framework to resolve. May be <see langword="null"/>.</param>
    /// <param name="majorVersion">When this method returns, contains the .NET major version number if resolution succeeded; otherwise 0.</param>
    /// <returns><see langword="true"/> if a major version could be determined; otherwise <see langword="false"/>.</returns>
    public static bool TryGetMajorVersion(this TargetFramework? targetFramework, out int majorVersion)
    {
        majorVersion = targetFramework switch
        {
            TargetFramework.Net8 => 8,
            TargetFramework.Net9 => 9,
            TargetFramework.Net10 => 10,
            TargetFramework.Net11 => 11,
            _ => 0
        };

        return majorVersion != 0;
    }
}
