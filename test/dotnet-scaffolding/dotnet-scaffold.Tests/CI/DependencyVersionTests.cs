// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;
using System.Xml.Linq;
using Microsoft.DotNet.Tools.Scaffold.Tests.Helpers;
using Xunit;

namespace Microsoft.DotNet.Tools.Scaffold.Tests.CI;

public class DependencyVersionTests
{
    [Fact]
    public void RoslynCentralVersionsShareOneSource()
    {
        string root = ScaffoldCliHelper.GetRepoRoot();
        var versions = XDocument.Load(Path.Combine(root, "eng", "Versions.props"));
        XNamespace ns = versions.Root!.Name.Namespace;
        Assert.Equal(
            "$(MicrosoftCodeAnalysisVersion)",
            Assert.Single(versions.Descendants(ns + "MicrosoftCodeAnalysisPreviewVersion")).Value);

        foreach (string path in new[]
        {
            Path.Combine(root, "Directory.Packages.props"),
            Path.Combine(root, "src", "dotnet-scaffolding", "Directory.Packages.props")
        })
        {
            var packages = XDocument.Load(path);
            Assert.All(
                packages.Descendants("PackageVersion"),
                package =>
                {
                    string? id = (string?)package.Attribute("Include");
                    if (id is "Microsoft.CodeAnalysis.Razor")
                    {
                        return;
                    }

                    if (id?.StartsWith("Microsoft.CodeAnalysis.", System.StringComparison.Ordinal) == true)
                    {
                        Assert.Contains((string?)package.Attribute("Version"),
                            new[] { "$(MicrosoftCodeAnalysisVersion)", "$(MicrosoftCodeAnalysisPreviewVersion)" });
                    }
                });
        }
    }
}
