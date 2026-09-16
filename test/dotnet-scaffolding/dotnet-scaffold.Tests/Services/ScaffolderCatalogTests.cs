// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.DotNet.Scaffolding.Core.ComponentModel;
using Microsoft.DotNet.Tools.Scaffold.Services;
using Moq;
using Xunit;

namespace Microsoft.DotNet.Tools.Scaffold.Tests.Services;

public class ScaffolderCatalogTests
{
    [Fact]
    public void GetCommands_CombinesProvidersAndPreservesComponentIdentity()
    {
        var firstComponent = CreateComponent("first", "one");
        var secondComponent = CreateComponent("second", "two");
        var firstProvider = new Mock<IScaffolderProvider>();
        var secondProvider = new Mock<IScaffolderProvider>();
        firstProvider.Setup(provider => provider.GetComponents()).Returns([firstComponent]);
        secondProvider.Setup(provider => provider.GetComponents()).Returns([secondComponent]);
        ScaffolderCatalog catalog = new([firstProvider.Object, secondProvider.Object]);

        var commands = catalog.GetCommands();

        Assert.Collection(
            commands,
            command =>
            {
                Assert.Equal("first", command.Key);
                Assert.Equal("one", command.Value.Name);
            },
            command =>
            {
                Assert.Equal("second", command.Key);
                Assert.Equal("two", command.Value.Name);
            });
        Assert.Same(secondComponent, catalog.FindComponent("SECOND"));
    }

    private static ScaffolderComponent CreateComponent(string componentName, string commandName)
        => new(
            new DotNetToolInfo
            {
                PackageName = componentName,
                Command = componentName,
                Version = "1.0.0"
            },
            [
                new CommandInfo
                {
                    Name = commandName,
                    DisplayName = commandName,
                    DisplayCategories = [],
                    Parameters = []
                }
            ]);
}
