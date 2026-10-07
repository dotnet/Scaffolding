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

public class AddIgniteUIServicesStepTests
{
    private static readonly string s_projectDirectory = Path.Combine(Path.GetTempPath(), "MyApp");
    private static readonly string s_projectPath = Path.Combine(s_projectDirectory, "MyApp.csproj");
    private readonly Mock<IFileSystem> _fileSystem = new();
    private readonly Mock<ILogger<WrappedCodeModificationStep>> _logger = new();

    private AddIgniteUIServicesStep CreateStep(string registrationFileName = "Program.cs", string? configPath = "igniteUIBlazorChanges.json")
        => new(_logger.Object, Mock.Of<ITelemetryService>(), _fileSystem.Object)
        {
            CodeChangeOptions = [],
            CodeModifierConfigPath = configPath,
            ProjectPath = s_projectPath,
            RegistrationFileName = registrationFileName
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
    public void IsRegistered_TrueWhenRegistrationFileCallsAddIgniteUIBlazor()
    {
        SetupFiles("MauiProgram.cs", ("MauiProgram.cs", "builder.Services.AddMauiBlazorWebView();\nbuilder.Services.AddIgniteUIBlazor();"));

        Assert.True(CreateStep("MauiProgram.cs").IsRegistered());
    }

    [Fact]
    public void IsRegistered_IgnoresBuildOutputAndOtherFiles()
    {
        SetupFiles("Program.cs",
            ("Program.cs", "var builder = WebApplication.CreateBuilder(args);\nvar app = builder.Build();"),
            (Path.Combine("obj", "Debug", "Program.cs"), "builder.Services.AddIgniteUIBlazor();"),
            (Path.Combine("bin", "Program.cs"), "builder.Services.AddIgniteUIBlazor();"));

        Assert.False(CreateStep().IsRegistered());
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
            invocation.Arguments[2].ToString()!.Contains("builder.Services.AddIgniteUIBlazor();") &&
            invocation.Arguments[2].ToString()!.Contains("re-run the scaffolder"));
    }
}
