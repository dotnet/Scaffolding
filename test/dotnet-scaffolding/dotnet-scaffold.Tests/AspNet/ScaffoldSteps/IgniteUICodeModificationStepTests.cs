// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.DotNet.Scaffolding.Core.Scaffolders;
using Microsoft.DotNet.Scaffolding.Internal.Services;
using Microsoft.DotNet.Scaffolding.Internal.Telemetry;
using Microsoft.DotNet.Tools.Scaffold.AspNet.ScaffoldSteps;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Microsoft.DotNet.Tools.Scaffold.Tests.AspNet.ScaffoldSteps;

public class IgniteUICodeModificationStepTests
{
    private static readonly string s_projectDirectory = Path.Combine(Path.GetTempPath(), "MyApp");
    private static readonly string s_projectPath = Path.Combine(s_projectDirectory, "MyApp.csproj");
    private readonly Mock<IFileSystem> _fileSystem = new();
    private readonly Mock<ILogger<WrappedCodeModificationStep>> _logger = new();

    private IgniteUICodeModificationStep CreateStep(string targetFileName = "Program.cs", string? configPath = "igniteUIBlazorChanges.json", params string[] requiredCalls)
        => new(_logger.Object, Mock.Of<ITelemetryService>(), _fileSystem.Object)
        {
            CodeChangeOptions = [],
            CodeModifierConfigPath = configPath,
            ProjectPath = s_projectPath,
            TargetFileName = targetFileName,
            RequiredCalls = requiredCalls.Length == 0 ? ["AddIgniteUIBlazor("] : requiredCalls,
            ChangeDescription = "'builder.Services.AddIgniteUIBlazor()'",
            ManualInstructions = "Add 'builder.Services.AddIgniteUIBlazor();'."
        };

    private void SetupFiles(string fileName, params (string RelativePath, string Content)[] files)
    {
        _fileSystem.Setup(fs => fs.DirectoryExists(s_projectDirectory)).Returns(true);
        _fileSystem.Setup(fs => fs.EnumerateFiles(s_projectDirectory, fileName, SearchOption.AllDirectories))
            .Returns(files.Select(file => Path.Combine(s_projectDirectory, file.RelativePath)).ToList());
        foreach (var (relativePath, content) in files)
        {
            _fileSystem.Setup(fs => fs.ReadAllText(Path.Combine(s_projectDirectory, relativePath))).Returns(content);
        }
    }

    [Fact]
    public void ContainsRequiredCalls_TrueWhenTargetFileCallsAddIgniteUIBlazor()
    {
        SetupFiles("MauiProgram.cs", ("MauiProgram.cs", "builder.Services.AddMauiBlazorWebView();\nbuilder.Services.AddIgniteUIBlazor();"));

        Assert.True(CreateStep("MauiProgram.cs").ContainsRequiredCalls());
    }

    [Fact]
    public void ContainsRequiredCalls_IgnoresBuildOutput()
    {
        SetupFiles("Program.cs",
            ("Program.cs", "var builder = WebApplication.CreateBuilder(args);\nvar app = builder.Build();"),
            (Path.Combine("obj", "Debug", "Program.cs"), "builder.Services.AddIgniteUIBlazor();"),
            (Path.Combine("bin", "Program.cs"), "builder.Services.AddIgniteUIBlazor();"));

        Assert.False(CreateStep().ContainsRequiredCalls());
    }

    [Fact]
    public void ContainsRequiredCalls_RequiresEveryCallInTheSameFile()
    {
        SetupFiles("Program.cs", ("Program.cs", "builder.Services.AddRazorComponents()\n    .AddInteractiveServerComponents();\napp.MapRazorComponents<App>();"));

        Assert.False(CreateStep(requiredCalls: ["AddInteractiveServerComponents(", "AddInteractiveServerRenderMode("]).ContainsRequiredCalls());
        Assert.True(CreateStep(requiredCalls: ["AddInteractiveServerComponents("]).ContainsRequiredCalls());
    }

    [Fact]
    public async Task ExecuteAsync_ReportsIncompleteSetup_WhenRegistrationCannotBeAdded()
    {
        // The shared code modification step fails here (no config), and the registration is not present.
        SetupFiles("Program.cs", ("Program.cs", "var app = WebApplication.Create(args);"));
        var step = CreateStep(configPath: null);

        Assert.False(await step.ExecuteAsync(new ScaffolderContext(Mock.Of<IScaffolder>())));

        Assert.False(step.ContinueOnError);
        Assert.Contains(_logger.Invocations, invocation =>
            invocation.Method.Name == nameof(ILogger.Log) &&
            Equals(invocation.Arguments[0], LogLevel.Error) &&
            invocation.Arguments[2].ToString()!.Contains("Ignite UI for Blazor setup is incomplete") &&
            invocation.Arguments[2].ToString()!.Contains("could not be added to Program.cs") &&
            invocation.Arguments[2].ToString()!.Contains("Add 'builder.Services.AddIgniteUIBlazor();'."));
    }

    [Fact]
    public async Task ExecuteAsync_ReportsIncompleteReason_WithoutModifyingAnything()
    {
        var step = CreateStep();
        step.IncompleteReason = "no host page was found";

        Assert.False(await step.ExecuteAsync(new ScaffolderContext(Mock.Of<IScaffolder>())));

        _fileSystem.Verify(fs => fs.WriteAllText(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        var error = Assert.Single(_logger.Invocations, invocation => Equals(invocation.Arguments[0], LogLevel.Error));
        Assert.Equal("Ignite UI for Blazor setup is incomplete: no host page was found. Add 'builder.Services.AddIgniteUIBlazor();'.", error.Arguments[2].ToString());
    }

    [Theory]
    [InlineData("<head><link href=\"_content/IgniteUI.Blazor/themes/dark/material.css\" rel=\"stylesheet\" /></head>", true)]
    [InlineData("<head></head>", false)]
    public void ContainsRequiredCalls_ChecksTheExactTargetFile(string hostPage, bool expected)
    {
        var hostPagePath = Path.Combine(s_projectDirectory, "Components", "App.razor");
        _fileSystem.Setup(fs => fs.FileExists(hostPagePath)).Returns(true);
        _fileSystem.Setup(fs => fs.ReadAllText(hostPagePath)).Returns(hostPage);
        var step = CreateStep("App.razor", requiredCalls: ["_content/IgniteUI.Blazor/themes/dark/material.css"]);
        step.TargetFilePath = hostPagePath;

        Assert.Equal(expected, step.ContainsRequiredCalls());
        _fileSystem.Verify(fs => fs.EnumerateFiles(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<SearchOption>()), Times.Never);
    }
}
