// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using Microsoft.DotNet.Scaffolding.Core.Builder;
using Microsoft.DotNet.Scaffolding.Core.Scaffolders;
using Microsoft.DotNet.Tools.Scaffold.Services;
using Moq;
using Xunit;

namespace Microsoft.DotNet.Tools.Scaffold.Tests.Services;

public class BuiltInScaffolderDiscoveryServiceTests
{
    [Fact]
    public void GetCommands_ReturnsMetadataFromConfiguredScaffolders()
    {
        var scaffolder = new Mock<IScaffolder>();
        scaffolder.SetupGet(s => s.Name).Returns("sample");
        scaffolder.SetupGet(s => s.DisplayName).Returns("Sample scaffolder");
        scaffolder.SetupGet(s => s.Categories).Returns(["All", "Sample"]);
        scaffolder.SetupGet(s => s.Description).Returns("Creates a sample.");
        scaffolder.SetupGet(s => s.Options).Returns([]);

        var scaffoldRunner = new Mock<IScaffoldRunner>();
        scaffoldRunner.SetupGet(r => r.Scaffolders).Returns(
            new Dictionary<ScaffolderCatagory, IEnumerable<IScaffolder>>
            {
                [ScaffolderCatagory.AspNet] = [scaffolder.Object]
            });

        var service = new BuiltInScaffolderDiscoveryService(scaffoldRunner.Object);

        var command = Assert.Single(service.GetCommands());
        Assert.Equal("dotnet-scaffold", command.Key);
        Assert.Equal("sample", command.Value.Name);
        Assert.Equal("Sample scaffolder", command.Value.DisplayName);
        Assert.Equal(["All", "Sample"], command.Value.DisplayCategories);
        Assert.Equal("Creates a sample.", command.Value.Description);
        Assert.Empty(command.Value.Parameters);
    }

    [Fact]
    public void GetCommands_WhenNoScaffoldersAreConfigured_ReturnsEmpty()
    {
        var scaffoldRunner = new Mock<IScaffoldRunner>();
        scaffoldRunner.SetupGet(r => r.Scaffolders).Returns(
            (IReadOnlyDictionary<ScaffolderCatagory, IEnumerable<IScaffolder>>?)null);

        var service = new BuiltInScaffolderDiscoveryService(scaffoldRunner.Object);

        Assert.Empty(service.GetCommands());
    }

    [Fact]
    public void Component_IdentifiesCurrentScaffoldTool()
    {
        var scaffoldRunner = new Mock<IScaffoldRunner>();
        var service = new BuiltInScaffolderDiscoveryService(scaffoldRunner.Object);

        Assert.Equal("Microsoft.dotnet-scaffold", service.Component.PackageName);
        Assert.Equal("dotnet-scaffold", service.Component.Command);
    }
}
