// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;
using System.Reflection;
using Xunit;

namespace Microsoft.DotNet.Tools.Scaffold.Tests.AspNet.Templates;

public class LegacyBlazorEditTemplateTests
{
    [Fact]
    public void LegacyEditTemplate_UsesPersistentComponentStatePattern()
    {
        string template = ReadLegacyEditTemplate();

        Assert.Contains("@inject PersistentComponentState ApplicationState", template);
        Assert.Contains("@implements IDisposable", template);
        Assert.DoesNotContain("[PersistentState]", template);
    }

    [Fact]
    public void LegacyEditTemplate_RegistersPersistCallbackBeforeAsyncQuery()
    {
        string template = ReadLegacyEditTemplate();

        int registerIndex = template.IndexOf("RegisterOnPersisting(PersistData)", StringComparison.Ordinal);
        int queryIndex = template.IndexOf("FirstOrDefaultAsync", StringComparison.Ordinal);

        Assert.True(registerIndex >= 0, "Expected callback registration in legacy Edit template.");
        Assert.True(queryIndex >= 0, "Expected database lookup in legacy Edit template.");
        Assert.True(registerIndex < queryIndex,
            "Legacy Edit template should register persistence callback before awaiting the database query.");
    }

    [Fact]
    public void LegacyEditTemplate_DoesNotPersistNullModelAndDisposesSafely()
    {
        string template = ReadLegacyEditTemplate();

        Assert.Contains("if (<#= modelName #> is not null)", template);
        Assert.Contains("PersistAsJson(nameof(<#= modelName #>), <#= modelName #>)", template);
        Assert.Contains("public void Dispose() => persistingSubscription?.Dispose();", template);
    }

    private static string ReadLegacyEditTemplate()
    {
        var repoRoot = GetRepoRoot();
        var templatePath = Path.Combine(repoRoot, "src", "Scaffolding", "VS.Web.CG.Mvc", "Templates", "Blazor", "Edit.tt");
        Assert.True(File.Exists(templatePath), $"Legacy template not found: {templatePath}");
        return File.ReadAllText(templatePath);
    }

    private static string GetRepoRoot()
    {
        var assemblyLocation = Assembly.GetExecutingAssembly().Location;
        var current = new DirectoryInfo(Path.GetDirectoryName(assemblyLocation)!);

        while (current is not null)
        {
            var candidate = Path.Combine(
                current.FullName,
                "src",
                "Scaffolding",
                "VS.Web.CG.Mvc",
                "Templates",
                "Blazor",
                "Edit.tt");

            if (File.Exists(candidate))
            {
                return current.FullName;
            }

            current = current.Parent;
        }

        throw new DirectoryNotFoundException("Could not locate repository root from test assembly path.");
    }
}
