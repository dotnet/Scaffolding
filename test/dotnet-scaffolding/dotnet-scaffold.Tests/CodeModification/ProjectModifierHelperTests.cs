// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.DotNet.Scaffolding.CodeModification.Helpers;
using Xunit;

namespace Microsoft.DotNet.Tools.Scaffold.Tests.CodeModification;

public class ProjectModifierHelperTests
{
    [Theory]
    [InlineData(null, new string[] { }, true)]
    [InlineData(new string[] { }, new string[] { "InteractiveServer" }, true)]
    [InlineData(new[] { "InteractiveServer" }, new[] { "interactiveserver" }, true)]
    [InlineData(new[] { "InteractiveServer" }, new string[] { }, false)]
    [InlineData(new[] { "!InteractiveServer" }, new string[] { }, true)]
    [InlineData(new[] { "!InteractiveServer" }, new[] { "interactiveserver" }, false)]
    [InlineData(new[] { "EfScenario", "!InteractiveServer" }, new[] { "efscenario", "InteractiveWebAssembly" }, true)]
    [InlineData(new[] { "EfScenario", "!InteractiveServer" }, new string[] { }, false)]
    [InlineData(new[] { "EfScenario", "!InteractiveServer" }, new[] { "EfScenario", "InteractiveServer" }, false)]
    [InlineData(new[] { "!InteractiveServer", "!InteractiveWebAssembly" }, new[] { "InteractiveWebAssembly" }, false)]
    public void FilterOptions_RequiresAllConditions(string[]? options, string[] codeChangeOptions, bool expected)
    {
        Assert.Equal(expected, ProjectModifierHelper.FilterOptions(options, codeChangeOptions));
    }
}
