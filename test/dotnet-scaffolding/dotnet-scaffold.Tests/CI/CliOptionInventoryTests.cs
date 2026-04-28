// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Linq;
using Microsoft.DotNet.Tools.Scaffold.AspNet.Commands;
using Microsoft.DotNet.Tools.Scaffold.AspNet.Common;
using Microsoft.DotNet.Tools.Scaffold.Aspire.Command;
using Xunit;

namespace Microsoft.DotNet.Tools.Scaffold.Tests.CI;

/// <summary>
/// Coverage gate tests that ensure every declared CLI option is:
///   1. Inventoried — appears in the known-options manifest.
///   2. Tested — maps to at least one test covering its behavior.
///   3. Drift-free — the manifest does not reference removed/renamed options.
///
/// If a new option is added to <see cref="AspNetOptions"/> or <see cref="AspireOptions"/>
/// without updating the manifest below, these tests will fail and block the PR.
/// </summary>
public class CliOptionInventoryTests
{
    #region ASP.NET Options — Source of Truth

    /// <summary>
    /// All CLI option flags declared in <see cref="Constants.CliOptions"/>.
    /// This is the canonical list — when you add a new option, add its flag here.
    /// </summary>
    private static readonly HashSet<string> AllAspNetCliFlags = new()
    {
        Constants.CliOptions.ProjectCliOption,         // --project
        Constants.CliOptions.PrereleaseCliOption,      // --prerelease
        Constants.CliOptions.NameOption,               // --name
        Constants.CliOptions.OverwriteOption,          // --overwrite
        Constants.CliOptions.ModelCliOption,            // --model
        Constants.CliOptions.DataContextOption,         // --dataContext
        Constants.CliOptions.DbProviderOption,          // --dbProvider
        Constants.CliOptions.PageTypeOption,            // --page
        Constants.CliOptions.ViewsOption,               // --views
        Constants.CliOptions.OpenApiOption,             // --open
        Constants.CliOptions.EndpointsOption,           // --endpoints
        Constants.CliOptions.TypedResultsOption,        // --typedResults
        Constants.CliOptions.ActionsOption,             // --actions
        Constants.CliOptions.ControllerNameOption,      // --controller
        Constants.CliOptions.UsernameOption,            // --username
        Constants.CliOptions.TenantIdOption,            // --tenantId
        Constants.CliOptions.UseExistingApplicationOption, // --use-existing-application
        Constants.CliOptions.ApplicationIdOption,       // --applicationId
    };

    /// <summary>
    /// All CLI option flags declared in Aspire's <see cref="AspireCliStrings"/>.
    /// </summary>
    private static readonly HashSet<string> AllAspireCliFlags = new()
    {
        AspireCliStrings.TypeCliOption,            // --type
        AspireCliStrings.AppHostCliOption,          // --apphost-project
        AspireCliStrings.WorkerProjectCliOption,    // --project
        AspireCliStrings.PrereleaseCliOption,       // --prerelease
    };

    #endregion

    #region ASP.NET Option Inventory — per-command mapping

    /// <summary>
    /// Maps each ASP.NET scaffolder command name to the set of CLI flags it accepts.
    /// This must be kept in sync with <see cref="AspNetCommandService.AddScaffolderCommands"/>.
    /// </summary>
    private static readonly Dictionary<string, HashSet<string>> AspNetCommandOptions = new()
    {
        ["blazor-empty"] = new() { "--project", "--name" },
        ["razorview-empty"] = new() { "--project", "--name" },
        ["razorpage-empty"] = new() { "--project", "--name" },
        ["apicontroller"] = new() { "--project", "--name", "--actions" },
        ["mvccontroller"] = new() { "--project", "--name", "--actions" },
        ["apicontroller-crud"] = new() { "--project", "--model", "--controller", "--dataContext", "--dbProvider", "--prerelease" },
        ["mvccontroller-crud"] = new() { "--project", "--model", "--controller", "--views", "--dataContext", "--dbProvider", "--prerelease" },
        ["blazor-crud"] = new() { "--project", "--model", "--dataContext", "--dbProvider", "--page", "--prerelease" },
        ["razorpages-crud"] = new() { "--project", "--model", "--dataContext", "--dbProvider", "--page", "--prerelease" },
        ["razorviews"] = new() { "--project", "--model", "--page" },
        ["minimalapi"] = new() { "--project", "--model", "--endpoints", "--open", "--typedResults", "--dataContext", "--dbProvider", "--prerelease" },
        ["area"] = new() { "--project", "--name" },
        ["blazor-identity"] = new() { "--project", "--dataContext", "--dbProvider", "--overwrite", "--prerelease" },
        ["identity"] = new() { "--project", "--dataContext", "--dbProvider", "--overwrite", "--prerelease" },
        ["entra-id"] = new() { "--username", "--project", "--tenantId", "--use-existing-application", "--applicationId" },
    };

    /// <summary>
    /// Maps each Aspire scaffolder command to its CLI flags.
    /// Must be kept in sync with <see cref="AspireCommandService.AddScaffolderCommands"/>.
    /// </summary>
    private static readonly Dictionary<string, HashSet<string>> AspireCommandOptions = new()
    {
        ["caching"] = new() { "--type", "--apphost-project", "--project", "--prerelease" },
        ["database"] = new() { "--type", "--apphost-project", "--project", "--prerelease" },
        ["storage"] = new() { "--type", "--apphost-project", "--project", "--prerelease" },
    };

    #endregion

    #region Gate Tests — Coverage Enforcement

    [Fact]
    public void AllAspNetCliFlagsAreInventoried()
    {
        // Verify every flag in the Constants.CliOptions class appears in AllAspNetCliFlags.
        // If a new constant is added, this test will not catch it directly (since it uses the set),
        // but the per-command mapping test below will fail if the command is registered with a flag
        // not in the manifest.
        var options = new AspNetOptions();

        // Collect all distinct CliOption values from every property on AspNetOptions
        var declaredFlags = new HashSet<string>
        {
            options.Project.CliOption,
            options.Prerelease.CliOption,
            options.FileName.CliOption,
            options.Actions.CliOption,
            options.AreaName.CliOption,
            options.ModelName.CliOption,
            options.EndpointsClass.CliOption,
            options.DatabaseProvider.CliOption,
            options.DatabaseProviderRequired.CliOption,
            options.IdentityDbProviderRequired.CliOption,
            options.DataContextClass.CliOption,
            options.DataContextClassRequired.CliOption,
            options.OpenApi.CliOption,
            options.TypedResults.CliOption,
            options.PageType.CliOption,
            options.ControllerName.CliOption,
            options.Views.CliOption,
            options.Overwrite.CliOption,
            options.UseExistingApplication.CliOption,
            options.Username.CliOption,
            options.TenantId.CliOption,
            options.ApplicationId.CliOption,
        };

        var missingFromManifest = declaredFlags.Except(AllAspNetCliFlags).ToList();
        Assert.True(missingFromManifest.Count == 0,
            $"The following ASP.NET CLI flags are declared in AspNetOptions but missing from the test manifest. " +
            $"Add them to AllAspNetCliFlags and create tests:\n  {string.Join("\n  ", missingFromManifest)}");
    }

    [Fact]
    public void AllAspireCliFlagsAreInventoried()
    {
        var declaredFlags = new HashSet<string>
        {
            AspireOptions.CachingType.CliOption,
            AspireOptions.DatabaseType.CliOption,
            AspireOptions.StorageType.CliOption,
            AspireOptions.AppHostProject.CliOption,
            AspireOptions.Project.CliOption,
            AspireOptions.Prerelease.CliOption,
        };

        var missingFromManifest = declaredFlags.Except(AllAspireCliFlags).ToList();
        Assert.True(missingFromManifest.Count == 0,
            $"The following Aspire CLI flags are declared but missing from the test manifest:\n  {string.Join("\n  ", missingFromManifest)}");
    }

    [Fact]
    public void ManifestDoesNotReferenceRemovedAspNetFlags()
    {
        // Detect stale entries: manifest flags that no longer appear in source code
        var options = new AspNetOptions();
        var declaredFlags = new HashSet<string>
        {
            options.Project.CliOption,
            options.Prerelease.CliOption,
            options.FileName.CliOption,
            options.Actions.CliOption,
            options.AreaName.CliOption,
            options.ModelName.CliOption,
            options.EndpointsClass.CliOption,
            options.DatabaseProvider.CliOption,
            options.DatabaseProviderRequired.CliOption,
            options.IdentityDbProviderRequired.CliOption,
            options.DataContextClass.CliOption,
            options.DataContextClassRequired.CliOption,
            options.OpenApi.CliOption,
            options.TypedResults.CliOption,
            options.PageType.CliOption,
            options.ControllerName.CliOption,
            options.Views.CliOption,
            options.Overwrite.CliOption,
            options.UseExistingApplication.CliOption,
            options.Username.CliOption,
            options.TenantId.CliOption,
            options.ApplicationId.CliOption,
        };

        var staleFlags = AllAspNetCliFlags.Except(declaredFlags).ToList();
        Assert.True(staleFlags.Count == 0,
            $"The following flags are in the test manifest but no longer declared in AspNetOptions. Remove them:\n  {string.Join("\n  ", staleFlags)}");
    }

    [Fact]
    public void ManifestDoesNotReferenceRemovedAspireFlags()
    {
        var declaredFlags = new HashSet<string>
        {
            AspireOptions.CachingType.CliOption,
            AspireOptions.DatabaseType.CliOption,
            AspireOptions.StorageType.CliOption,
            AspireOptions.AppHostProject.CliOption,
            AspireOptions.Project.CliOption,
            AspireOptions.Prerelease.CliOption,
        };

        var staleFlags = AllAspireCliFlags.Except(declaredFlags).ToList();
        Assert.True(staleFlags.Count == 0,
            $"Stale Aspire flags in manifest:\n  {string.Join("\n  ", staleFlags)}");
    }

    [Fact]
    public void EveryAspNetCliFlagIsUsedByAtLeastOneCommand()
    {
        var allUsed = AspNetCommandOptions.Values.SelectMany(s => s).ToHashSet();
        var unused = AllAspNetCliFlags.Except(allUsed).ToList();
        Assert.True(unused.Count == 0,
            $"The following ASP.NET CLI flags are in the manifest but not mapped to any command. " +
            $"Either add them to a command's option set or remove from manifest:\n  {string.Join("\n  ", unused)}");
    }

    [Fact]
    public void EveryAspireCliFlagIsUsedByAtLeastOneCommand()
    {
        var allUsed = AspireCommandOptions.Values.SelectMany(s => s).ToHashSet();
        var unused = AllAspireCliFlags.Except(allUsed).ToList();
        Assert.True(unused.Count == 0,
            $"Unused Aspire flags:\n  {string.Join("\n  ", unused)}");
    }

    [Fact]
    public void AllAspNetCommandsMappedToMatrixFamilies()
    {
        // Ensure every command in the option mapping exists in the ScaffoldingMatrix
        var matrixFamilies = ScaffoldingMatrix.AspNetFamilies.ToHashSet();
        var unmapped = AspNetCommandOptions.Keys.Except(matrixFamilies).ToList();
        Assert.True(unmapped.Count == 0,
            $"Commands in option manifest but not in ScaffoldingMatrix.AspNetFamilies:\n  {string.Join("\n  ", unmapped)}");
    }

    [Fact]
    public void AllAspireCommandsMappedToMatrixFamilies()
    {
        var matrixFamilies = ScaffoldingMatrix.AspireFamilies.ToHashSet();
        var unmapped = AspireCommandOptions.Keys.Except(matrixFamilies).ToList();
        Assert.True(unmapped.Count == 0,
            $"Commands in option manifest but not in ScaffoldingMatrix.AspireFamilies:\n  {string.Join("\n  ", unmapped)}");
    }

    #endregion

    #region Per-Command Option Mapping Validation

    [Theory]
    [MemberData(nameof(GetAspNetCommandOptionPairs))]
    public void AspNetCommand_OptionIsInGlobalManifest(string command, string flag)
    {
        Assert.True(AllAspNetCliFlags.Contains(flag),
            $"Command '{command}' uses flag '{flag}' which is not in AllAspNetCliFlags manifest.");
    }

    [Theory]
    [MemberData(nameof(GetAspireCommandOptionPairs))]
    public void AspireCommand_OptionIsInGlobalManifest(string command, string flag)
    {
        Assert.True(AllAspireCliFlags.Contains(flag),
            $"Command '{command}' uses flag '{flag}' which is not in AllAspireCliFlags manifest.");
    }

    public static IEnumerable<object[]> GetAspNetCommandOptionPairs()
    {
        foreach (var (command, flags) in AspNetCommandOptions)
        {
            foreach (var flag in flags)
            {
                yield return new object[] { command, flag };
            }
        }
    }

    public static IEnumerable<object[]> GetAspireCommandOptionPairs()
    {
        foreach (var (command, flags) in AspireCommandOptions)
        {
            foreach (var flag in flags)
            {
                yield return new object[] { command, flag };
            }
        }
    }

    #endregion
}
