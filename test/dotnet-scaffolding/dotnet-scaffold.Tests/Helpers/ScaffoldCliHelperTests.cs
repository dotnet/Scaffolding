// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;
using System.Linq;
using System.Xml.Linq;
using Xunit;

namespace Microsoft.DotNet.Tools.Scaffold.Tests.Helpers;

public class ScaffoldCliHelperTests
{
    [Fact]
    public void PreviewNuGetConfig_ContainsOnlyApplicationPackageFeeds()
    {
        var config = XDocument.Parse(ScaffoldCliHelper.PreviewNuGetConfig);
        var sources = config.Root!.Element("packageSources")!;

        Assert.NotNull(sources.Element("clear"));
        Assert.Equal(
            new[] { "dotnet-public", "dotnet11", "dotnet11-transport" },
            sources.Elements("add").Select(source => source.Attribute("key")!.Value));
        Assert.NotNull(config.Root.Element("disabledPackageSources")!.Element("clear"));
    }

    [Fact]
    public void PreviewNuGetConfig_UsesRepositoryFeedUrls()
    {
        var repositoryConfig = XDocument.Load(Path.Combine(ScaffoldCliHelper.GetRepoRoot(), "NuGet.config"));
        var config = XDocument.Parse(ScaffoldCliHelper.PreviewNuGetConfig);

        foreach (var source in config.Root!.Element("packageSources")!.Elements("add"))
        {
            var repositorySource = Assert.Single(
                repositoryConfig.Root!.Element("packageSources")!.Elements("add"),
                candidate => candidate.Attribute("key")!.Value == source.Attribute("key")!.Value);
            Assert.Equal(repositorySource.Attribute("value")!.Value, source.Attribute("value")!.Value);
        }
    }
}
