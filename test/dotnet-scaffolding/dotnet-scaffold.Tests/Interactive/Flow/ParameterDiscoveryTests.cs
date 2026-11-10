// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
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

    [Theory]
    [InlineData("views", true)]
    [InlineData("VIEWS", true)]
    [InlineData("minimalapi", false)]
    [InlineData("razorpages-crud", false)]
    [InlineData("mvccontroller", false)]
    [InlineData("blazor-crud", false)]
    [InlineData("blazor-identity", false)]
    public async Task GetClassDisplayNamesAsync_UsesQualifiedIdentitiesOnlyForViews(string commandName, bool qualified)
    {
        var compilation = CSharpCompilation.Create("TestProject", [CSharpSyntaxTree.ParseText("""
            namespace WebApp { public class Book { } }
            namespace Library.Models { public class Book { } }
            """)]);
        var localBook = Assert.IsAssignableFrom<INamedTypeSymbol>(compilation.GetTypeByMetadataName("WebApp.Book"));
        var referencedBook = Assert.IsAssignableFrom<INamedTypeSymbol>(compilation.GetTypeByMetadataName("Library.Models.Book"));
        var codeService = new Mock<ICodeService>();
        codeService.Setup(service => service.GetAllClassSymbolsAsync()).ReturnsAsync(new List<ISymbol> { localBook });
        codeService.Setup(service => service.GetAllClassSymbolsAsync(true)).ReturnsAsync(new List<ISymbol> { localBook, referencedBook });
        var command = new CommandInfo
        {
            Name = commandName,
            DisplayName = commandName,
            DisplayCategories = [],
            Parameters = []
        };

        var options = await ParameterDiscovery.GetClassDisplayNamesAsync(codeService.Object, command);

        Assert.Equal(qualified ? 2 : 1, options.Count);
        Assert.Equal(qualified ? "global::WebApp.Book" : "Book", options[0].Name);
        Assert.Equal(qualified ? "global::WebApp.Book" : "Book", options[0].Value);
        if (qualified)
        {
            Assert.Equal("global::Library.Models.Book", options[1].Name);
            Assert.Equal("global::Library.Models.Book", options[1].Value);
        }
    }
}
