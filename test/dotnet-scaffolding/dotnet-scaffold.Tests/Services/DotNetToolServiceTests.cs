// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.DotNet.Scaffolding.Core.ComponentModel;
using Microsoft.DotNet.Tools.Scaffold.Services;
using Xunit;

namespace Microsoft.DotNet.Tools.Scaffold.Tests.Services;

public class DotNetToolServiceTests
{
    [Theory]
    [InlineData("Microsoft.dotnet-scaffold", true)]
    [InlineData("microsoft.dotnet-scaffold", true)]
    [InlineData("redth.mauidevflow.cli", false)]
    [InlineData("QuestPDF.Previewer", false)]
    public void IsDotNetScaffoldTool_IdentifiesOnlyDotNetScaffold(string packageName, bool expected)
    {
        DotNetToolInfo tool = new()
        {
            PackageName = packageName,
            Version = "1.0.0",
            Command = "test"
        };

        Assert.Equal(expected, DotNetToolService.IsDotNetScaffoldTool(tool));
    }
}
