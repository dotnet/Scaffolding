// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.DotNet.Scaffolding.Core.ComponentModel;
using Microsoft.DotNet.Scaffolding.Roslyn.Services;
using Microsoft.DotNet.Tools.Scaffold.Interactive.Flow.Steps;
using Moq;
using Xunit;

namespace Microsoft.DotNet.Tools.Scaffold.Tests.Interactive.Flow;

public class ParameterDiscoveryTests
{
    [Theory]
    [InlineData("views", true)]
    [InlineData("minimalapi", false)]
    [InlineData("razorpages-crud", false)]
    [InlineData("mvccontroller", false)]
    [InlineData("blazor-crud", false)]
    [InlineData("blazor-identity", false)]
    [InlineData(null, false)]
    public async Task GetClassDisplayNamesAsync_OnlyExpandsRazorViewsDiscovery(string? commandName, bool includeReferences)
    {
        var codeService = new Mock<ICodeService>();
        codeService.Setup(service => service.GetAllClassSymbolsAsync()).ReturnsAsync(new List<ISymbol>());
        codeService.Setup(service => service.GetAllClassSymbolsAsync(true)).ReturnsAsync(new List<ISymbol>());
        CommandInfo? command = commandName is null ? null : new CommandInfo
        {
            Name = commandName,
            DisplayName = commandName,
            DisplayCategories = [],
            Parameters = []
        };

        var options = await ParameterDiscovery.GetClassDisplayNamesAsync(codeService.Object, command);

        Assert.Empty(options);
        codeService.Verify(service => service.GetAllClassSymbolsAsync(true), includeReferences ? Times.Once() : Times.Never());
        codeService.Verify(service => service.GetAllClassSymbolsAsync(), includeReferences ? Times.Never() : Times.Once());
    }
}
