// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.DotNet.Scaffolding.Core.Scaffolders;
using Microsoft.DotNet.Scaffolding.Internal.Services;
using Microsoft.DotNet.Tools.Scaffold.AspNet.ScaffoldSteps;
using Microsoft.DotNet.Tools.Scaffold.Tests.Helpers;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace Microsoft.DotNet.Tools.Scaffold.Tests.AspNet.ScaffoldSteps;

public class WrappedCodeModificationStepTests
{
    private readonly Mock<IScaffolder> _mockScaffolder;
    private readonly ScaffolderContext _context;

    public WrappedCodeModificationStepTests()
    {
        _mockScaffolder = new Mock<IScaffolder>();
        
        _mockScaffolder.Setup(s => s.DisplayName).Returns("TestScaffolder");
        _mockScaffolder.Setup(s => s.Name).Returns("test-scaffolder");
        _context = new ScaffolderContext(_mockScaffolder.Object);
    }

    [Fact]
    public void Constructor_InitializesCorrectly()
    {
        // Arrange
        Mock<ITelemetryService> mockTelemetryService = new Mock<ITelemetryService>();

        // Act
        WrappedCodeModificationStep step = new WrappedCodeModificationStep(
            NullLogger<WrappedCodeModificationStep>.Instance,
            mockTelemetryService.Object)
        {
            CodeChangeOptions = [],
            ProjectPath = string.Empty
        };

        // Assert
        Assert.NotNull(step);
    }

    [Fact]
    public async Task ExecuteAsync_ReturnsFalse_WhenNoCodeModifierConfigProvided()
    {
        // Arrange
        Mock<ITelemetryService> mockTelemetryService = new Mock<ITelemetryService>();
        WrappedCodeModificationStep step = new WrappedCodeModificationStep(
            NullLogger<WrappedCodeModificationStep>.Instance,
            mockTelemetryService.Object)
        {
            CodeChangeOptions = [],
            ProjectPath = string.Empty
        };

        // Act
        bool result = await step.ExecuteAsync(_context, CancellationToken.None);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task ExecuteAsync_TracksTelemetry()
    {
        // Arrange
        Mock<ITelemetryService> mockTelemetryService = new Mock<ITelemetryService>();
        WrappedCodeModificationStep step = new WrappedCodeModificationStep(
            NullLogger<WrappedCodeModificationStep>.Instance,
            mockTelemetryService.Object)
        {
            CodeChangeOptions = [],
            ProjectPath = string.Empty
        };

        // Act
        bool result = await step.ExecuteAsync(_context, CancellationToken.None);

        // Assert
        Assert.False(result);
        // Verify telemetry was tracked
        mockTelemetryService.Verify(
            ts => ts.TrackEvent(
                It.IsAny<string>(),
                It.IsAny<System.Collections.Generic.IReadOnlyDictionary<string, string>>(),
                It.IsAny<System.Collections.Generic.IReadOnlyDictionary<string, double>>()),
            Times.Once);
    }

    [Theory]
    [InlineData("""{"FileBlock":"missing.razor"}""", false, "missing.razor")]
    [InlineData("""{"FileBlock":"block.razor","Block":"text"}""", false, "cannot be combined")]
    [InlineData("""{"FileBlock":"block.razor","MultiLineBlock":["text"]}""", false, "cannot be combined")]
    [InlineData("""{"FileBlock":"block.razor"}""", true, "requires a file-based configuration")]
    public async Task ExecuteAsync_RejectsInvalidFileBlock(string snippet, bool inlineConfig, string expectedDiagnostic)
    {
        var directory = Path.Combine(Path.GetTempPath(), nameof(WrappedCodeModificationStepTests), Guid.NewGuid().ToString());
        Directory.CreateDirectory(directory);
        try
        {
            var configPath = Path.Combine(directory, "changes.json");
            var projectPath = Path.Combine(directory, "TestProject.csproj");
            var config = $$"""
                {"Files":[{"FileName":"Program.cs","Replacements":[{{snippet}}]}]}
                """;
            File.WriteAllText(configPath, config);
            File.WriteAllText(projectPath, "<Project />");
            var logger = new Mock<ILogger<WrappedCodeModificationStep>>();
            var step = new WrappedCodeModificationStep(logger.Object, Mock.Of<ITelemetryService>())
            {
                CodeModifierConfigPath = inlineConfig ? null : configPath,
                CodeModifierConfigJsonText = inlineConfig ? config : null,
                CodeChangeOptions = [],
                ProjectPath = projectPath
            };

            Assert.False(await step.ExecuteAsync(_context));
            Assert.Contains(logger.Invocations, invocation =>
                invocation.Method.Name == nameof(ILogger.Log) &&
                Equals(invocation.Arguments[0], LogLevel.Error) &&
                invocation.Arguments[2].ToString()!.Contains(expectedDiagnostic));
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task BlazorCrudWebAssemblyMigrationsEndpoint_IsDevelopmentOnlyAndNotDuplicated(bool previouslyScaffolded)
    {
        var configPath = Path.Combine(ScaffoldCliHelper.GetRepoRoot(), "src", "dotnet-scaffolding", "dotnet-scaffold",
            "AspNet", "Templates", "net11.0", "CodeModificationConfigs", "blazorWebCrudChanges.json");
        using var config = JsonDocument.Parse(File.ReadAllText(configPath));
        var replacements = config.RootElement.GetProperty("Files").EnumerateArray()
            .Single(file => file.GetProperty("FileName").GetString() == "Program.cs")
            .GetProperty("Replacements").EnumerateArray()
            .Where(replacement => replacement.ToString().Contains("UseMigrationsEndPoint")).ToArray();

        using var project = new BlazorTestProject(ScaffoldCliHelper.GetTestTargetFramework());
        var programPath = Path.Combine(project.ProjectDirectory, "Program.cs");
        File.WriteAllText(programPath, ScaffoldCliHelper.GetMinimalProgramCs());
        var build = await ScaffoldCliHelper.RunBuildAsync(project.ProjectDirectory);
        Assert.True(build.ExitCode == 0, $"Test project build failed.\n{build.Output}\n{build.Error}");

        var program = """
            var builder = WebApplication.CreateBuilder(args);
            var app = builder.Build();
            if (app.Environment.IsDevelopment())
            {
                app.UseWebAssemblyDebugging();
            }
            else
            {
                app.UseExceptionHandler("/Error");
                app.UseHsts();
            }
            app.UseHttpsRedirection();
            app.Run();
            """;
        File.WriteAllText(programPath, program.Replace("\r\n", "\n"));
        if (previouslyScaffolded)
        {
            var previousStep = new WrappedCodeModificationStep(NullLogger<WrappedCodeModificationStep>.Instance, Mock.Of<ITelemetryService>())
            {
                ProjectPath = project.ProjectPath,
                CodeChangeOptions = [],
                CodeModifierConfigJsonText = """
                    {"Files":[{"FileName":"Program.cs","Replacements":[{
                        "ReplaceSnippet":["app.UseHsts()"],
                        "MultiLineBlock":["app.UseHsts();","    app.UseMigrationsEndPoint()"]
                    }]}]}
                    """
            };
            Assert.True(await previousStep.ExecuteAsync(_context));
            Assert.Contains($"app.UseHsts();{Environment.NewLine}    app.UseMigrationsEndPoint();\n}}", File.ReadAllText(programPath));
        }

        var step = new WrappedCodeModificationStep(NullLogger<WrappedCodeModificationStep>.Instance, Mock.Of<ITelemetryService>())
        {
            ProjectPath = project.ProjectPath,
            CodeChangeOptions = [],
            CodeModifierConfigJsonText = JsonSerializer.Serialize(new
            {
                Files = new[] { new { FileName = "Program.cs", Replacements = replacements } }
            })
        };

        Assert.True(await step.ExecuteAsync(_context));
        var updatedProgram = File.ReadAllText(programPath);
        var root = CSharpSyntaxTree.ParseText(updatedProgram).GetRoot();
        Assert.Empty(root.GetDiagnostics());
        var migration = Assert.Single(root.DescendantNodes().OfType<InvocationExpressionSyntax>(),
            invocation => invocation.Expression.ToString() == "app.UseMigrationsEndPoint");
        var development = Assert.Single(migration.Ancestors().OfType<IfStatementSyntax>());
        Assert.Equal("app.Environment.IsDevelopment()", development.Condition.ToString());
        Assert.Contains(migration, development.Statement.DescendantNodes());
        Assert.Contains("app.UseHsts();", updatedProgram);
        Assert.Contains("app.UseExceptionHandler(\"/Error\");", updatedProgram);
        Assert.Contains("app.UseWebAssemblyDebugging();", updatedProgram);

        Assert.True(await step.ExecuteAsync(_context));
        Assert.Equal(updatedProgram, File.ReadAllText(programPath));
    }
}
