// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.DotNet.Scaffolding.Core.Scaffolders;
using Microsoft.DotNet.Scaffolding.Internal.Services;
using Microsoft.DotNet.Tools.Scaffold.AspNet.ScaffoldSteps;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace Microsoft.DotNet.Tools.Scaffold.Tests.AspNet.ScaffoldSteps;

public class AddRazorImportsStepTests
{
    private const string Namespace = "IgniteUI.Blazor.Controls";
    private readonly Mock<IFileSystem> _mockFileSystem = new();
    private readonly Mock<ITelemetryService> _mockTelemetryService = new();
    private readonly ScaffolderContext _context;
    private readonly string _importsPath = Path.Combine("C:", "src", "MyApp", "Components", "_Imports.razor");

    public AddRazorImportsStepTests()
    {
        var mockScaffolder = new Mock<IScaffolder>();
        mockScaffolder.Setup(s => s.DisplayName).Returns("TestScaffolder");
        mockScaffolder.Setup(s => s.Name).Returns("test-scaffolder");
        _context = new ScaffolderContext(mockScaffolder.Object);
    }

    private AddRazorImportsStep CreateStep(string importsPath, params string[] namespaces)
        => new(NullLogger<AddRazorImportsStep>.Instance, _mockFileSystem.Object, _mockTelemetryService.Object)
        {
            ImportsFilePath = importsPath,
            Namespaces = namespaces
        };

    [Fact]
    public async Task ExecuteAsync_ReturnsFalse_WhenImportsPathIsEmpty()
    {
        var step = CreateStep(string.Empty, Namespace);

        bool result = await step.ExecuteAsync(_context, CancellationToken.None);

        Assert.False(result);
        _mockFileSystem.Verify(f => f.WriteAllText(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task ExecuteAsync_CreatesFile_WhenImportsFileDoesNotExist()
    {
        _mockFileSystem.Setup(f => f.FileExists(_importsPath)).Returns(false);
        string? written = null;
        _mockFileSystem.Setup(f => f.WriteAllText(_importsPath, It.IsAny<string>())).Callback<string, string>((_, c) => written = c);
        var step = CreateStep(_importsPath, Namespace);

        bool result = await step.ExecuteAsync(_context, CancellationToken.None);

        Assert.True(result);
        _mockFileSystem.Verify(f => f.CreateDirectoryIfNotExists(Path.GetDirectoryName(_importsPath)!), Times.Once);
        Assert.NotNull(written);
        Assert.Contains($"@using {Namespace}", written);
    }

    [Fact]
    public async Task ExecuteAsync_AppendsUsing_WhenMissing()
    {
        _mockFileSystem.Setup(f => f.FileExists(_importsPath)).Returns(true);
        _mockFileSystem.Setup(f => f.ReadAllText(_importsPath)).Returns("@using Microsoft.AspNetCore.Components.Forms\n@using Microsoft.AspNetCore.Components.Web\n");
        string? written = null;
        _mockFileSystem.Setup(f => f.WriteAllText(_importsPath, It.IsAny<string>())).Callback<string, string>((_, c) => written = c);
        var step = CreateStep(_importsPath, Namespace);

        bool result = await step.ExecuteAsync(_context, CancellationToken.None);

        Assert.True(result);
        Assert.Equal("@using Microsoft.AspNetCore.Components.Forms\n@using Microsoft.AspNetCore.Components.Web\n@using IgniteUI.Blazor.Controls\n", written);
    }

    [Fact]
    public async Task ExecuteAsync_DoesNotWrite_WhenUsingAlreadyPresent()
    {
        _mockFileSystem.Setup(f => f.FileExists(_importsPath)).Returns(true);
        _mockFileSystem.Setup(f => f.ReadAllText(_importsPath)).Returns("@using Microsoft.AspNetCore.Components.Web\n@using IgniteUI.Blazor.Controls\n");
        var step = CreateStep(_importsPath, Namespace);

        bool result = await step.ExecuteAsync(_context, CancellationToken.None);

        Assert.True(result);
        _mockFileSystem.Verify(f => f.WriteAllText(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task ExecuteAsync_ReturnsTrue_WhenNoNamespacesRequested()
    {
        var step = CreateStep(_importsPath);

        bool result = await step.ExecuteAsync(_context, CancellationToken.None);

        Assert.True(result);
        _mockFileSystem.Verify(f => f.WriteAllText(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task ExecuteAsync_TracksTelemetryEvent()
    {
        _mockFileSystem.Setup(f => f.FileExists(_importsPath)).Returns(true);
        _mockFileSystem.Setup(f => f.ReadAllText(_importsPath)).Returns(string.Empty);
        var step = CreateStep(_importsPath, Namespace);

        await step.ExecuteAsync(_context, CancellationToken.None);

        _mockTelemetryService.Verify(t => t.TrackEvent(
            It.Is<string>(name => name.Contains(nameof(AddRazorImportsStep))),
            It.IsAny<IReadOnlyDictionary<string, string>>(),
            It.IsAny<IReadOnlyDictionary<string, double>>()), Times.Once);
    }

    [Fact]
    public void AddUsings_PreservesCrlfLineEndings()
    {
        var content = "@using A\r\n@using B\r\n";

        var updated = AddRazorImportsStep.AddUsings(content, [Namespace], out var added);

        Assert.Equal("@using A\r\n@using B\r\n@using IgniteUI.Blazor.Controls\r\n", updated);
        Assert.Equal(new[] { Namespace }, added);
    }

    [Fact]
    public void AddUsings_AddsLineBreak_WhenContentDoesNotEndWithNewline()
    {
        var updated = AddRazorImportsStep.AddUsings("@using A", [Namespace], out _);

        Assert.Equal("@using A\n@using IgniteUI.Blazor.Controls\n", updated);
    }

    [Fact]
    public void AddUsings_AddsOnlyMissingNamespaces()
    {
        var updated = AddRazorImportsStep.AddUsings("@using A\n", ["A", "B", "C"], out var added);

        Assert.Equal("@using A\n@using B\n@using C\n", updated);
        Assert.Equal(new[] { "B", "C" }, added);
    }

    [Theory]
    [InlineData("@using IgniteUI.Blazor.Controls", true)]
    [InlineData("  @using   IgniteUI.Blazor.Controls  ", true)]
    [InlineData("@using IgniteUI.Blazor.Controls;", true)]
    [InlineData("@using Microsoft.AspNetCore.Components.Web\r\n@using IgniteUI.Blazor.Controls\r\n", true)]
    [InlineData("@using IgniteUI.Blazor.Controls.Extra", false)]
    [InlineData("@using IgniteUI.Blazor", false)]
    [InlineData("@* @using IgniteUI.Blazor.Controls *@", false)]
    [InlineData("", false)]
    [InlineData(null, false)]
    public void ContainsUsing_MatchesWholeDirectiveOnly(string? content, bool expected)
        => Assert.Equal(expected, AddRazorImportsStep.ContainsUsing(content, Namespace));
}
