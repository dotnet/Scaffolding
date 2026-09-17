// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.DotNet.Scaffolding.Core.Scaffolders;
using Microsoft.DotNet.Tools.Scaffold.Services;
using Moq;
using Xunit;

namespace Microsoft.DotNet.Tools.Scaffold.Tests.Services;

public class BuiltInScaffolderMetadataProviderTests
{
    [Fact]
    public void GetComponents_ReturnsMetadataFromConfiguredScaffolders()
    {
        var scaffolder = new Mock<IScaffolder>();
        scaffolder.SetupGet(s => s.Name).Returns("sample");
        scaffolder.SetupGet(s => s.DisplayName).Returns("Sample scaffolder");
        scaffolder.SetupGet(s => s.Categories).Returns(["All", "Sample"]);
        scaffolder.SetupGet(s => s.Description).Returns("Creates a sample.");
        scaffolder.SetupGet(s => s.Options).Returns([]);

        var provider = new BuiltInScaffolderMetadataProvider([scaffolder.Object]);

        var component = Assert.Single(provider.GetComponents());
        var command = Assert.Single(component.Commands);
        Assert.Equal("Microsoft.dotnet-scaffold", component.Component.PackageName);
        Assert.Equal("dotnet-scaffold", component.Component.Command);
        Assert.Equal("sample", command.Name);
        Assert.Equal("Sample scaffolder", command.DisplayName);
        Assert.Equal(["All", "Sample"], command.DisplayCategories);
        Assert.Equal("Creates a sample.", command.Description);
        Assert.Empty(command.Parameters);
    }
}
