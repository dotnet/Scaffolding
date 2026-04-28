// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Linq;

namespace Microsoft.DotNet.Tools.Scaffold.Tests.CI;

/// <summary>
/// Single source of truth for the CI scaffolding permutation matrix.
/// Defines which scaffold families are tested against which target frameworks,
/// and which combinations are explicitly unsupported.
/// 
/// To add a new framework or scaffolder family, update the arrays below and
/// add corresponding integration tests. The <see cref="CliOptionInventoryTests"/>
/// coverage gate will fail if any new options are introduced without test coverage.
/// </summary>
public static class ScaffoldingMatrix
{
    /// <summary>Target frameworks under test.</summary>
    public static readonly string[] TargetFrameworks = ["net8.0", "net9.0", "net10.0", "net11.0"];

    /// <summary>ASP.NET scaffolder families.</summary>
    public static readonly string[] AspNetFamilies =
    [
        "blazor-empty",
        "blazor-crud",
        "razorview-empty",
        "razorviews",
        "razorpage-empty",
        "razorpages-crud",
        "mvccontroller",
        "mvccontroller-crud",
        "apicontroller",
        "apicontroller-crud",
        "minimalapi",
        "area",
        "blazor-identity",
        "identity",
        "entra-id"
    ];

    /// <summary>Aspire scaffolder families.</summary>
    public static readonly string[] AspireFamilies =
    [
        "caching",
        "database",
        "storage"
    ];

    /// <summary>All scaffolder families (ASP.NET + Aspire).</summary>
    public static IEnumerable<string> AllFamilies => AspNetFamilies.Concat(AspireFamilies);

    /// <summary>
    /// Combinations that are explicitly unsupported and must be excluded from CI matrix.
    /// Format: (Family, Framework).
    /// </summary>
    public static readonly HashSet<(string Family, string Framework)> UnsupportedCombinations = new()
    {
        // EntraID scaffolder only supports net10.0+
        ("entra-id", "net8.0"),
        ("entra-id", "net9.0"),
    };

    /// <summary>
    /// Returns true if the given combination is supported for CI testing.
    /// </summary>
    public static bool IsSupported(string family, string framework)
        => !UnsupportedCombinations.Contains((family, framework));

    /// <summary>
    /// Returns all supported (Family, Framework) tuples for matrix generation.
    /// </summary>
    public static IEnumerable<(string Family, string Framework)> GetSupportedPermutations()
    {
        foreach (var family in AllFamilies)
        {
            foreach (var tfm in TargetFrameworks)
            {
                if (IsSupported(family, tfm))
                {
                    yield return (family, tfm);
                }
            }
        }
    }
}
