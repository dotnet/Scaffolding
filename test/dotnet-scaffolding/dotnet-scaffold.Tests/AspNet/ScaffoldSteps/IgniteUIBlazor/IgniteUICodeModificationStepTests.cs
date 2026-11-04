// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using System.IO;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Text;
using Microsoft.DotNet.Scaffolding.Core.Scaffolders;
using Microsoft.DotNet.Scaffolding.Internal.Services;
using Microsoft.DotNet.Scaffolding.Roslyn.Services;
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
    private readonly Mock<ICodeService> _codeService = new();
    private readonly Mock<ILogger<WrappedCodeModificationStep>> _logger = new();

    private IgniteUICodeModificationStep CreateStep(string targetFileName = "Program.cs", string? configPath = "igniteUIBlazorChanges.json", params string[] requiredCalls)
        => new(_logger.Object, Mock.Of<ITelemetryService>(), _fileSystem.Object)
        {
            CodeChangeOptions = [],
            CodeModifierConfigPath = configPath,
            ProjectPath = s_projectPath,
            TargetFileName = targetFileName,
            CodeService = _codeService.Object,
            RequiredCalls = requiredCalls.Length == 0 ? ["AddIgniteUIBlazor"] : requiredCalls,
            ChangeDescription = "'builder.Services.AddIgniteUIBlazor()'",
            ManualInstructions = "Add 'builder.Services.AddIgniteUIBlazor();'."
        };

    /// <summary>
    /// Sets up the project's compiled documents, which are also files on disk.
    /// </summary>
    private void SetupDocuments(params (string RelativePath, string Content)[] documents)
        => SetupDocuments(parseOptions: null, documents);

    /// <summary>
    /// Sets up the project's compiled documents, parsed with <paramref name="parseOptions"/>, which are also files on disk.
    /// </summary>
    private void SetupDocuments(CSharpParseOptions? parseOptions, params (string RelativePath, string Content)[] documents)
    {
        var workspace = new AdhocWorkspace();
        var projectId = workspace.AddProject(ProjectInfo.Create(ProjectId.CreateNewId(), VersionStamp.Create(), "MyApp", "MyApp", LanguageNames.CSharp, filePath: s_projectPath, parseOptions: parseOptions)).Id;
        foreach (var (relativePath, content) in documents)
        {
            var path = Path.Combine(s_projectDirectory, relativePath);
            workspace.AddDocument(DocumentInfo.Create(DocumentId.CreateNewId(projectId), Path.GetFileName(path), filePath: path,
                loader: TextLoader.From(TextAndVersion.Create(SourceText.From(content), VersionStamp.Create()))));
            SetupFileOnDisk(relativePath, content);
        }

        _codeService.Setup(codeService => codeService.GetWorkspaceAsync()).ReturnsAsync(workspace);
    }

    private void SetupFileOnDisk(string relativePath, string content)
    {
        var path = Path.Combine(s_projectDirectory, relativePath);
        _fileSystem.Setup(fs => fs.FileExists(path)).Returns(true);
        _fileSystem.Setup(fs => fs.ReadAllText(path)).Returns(content);
    }

    [Fact]
    public async Task ContainsRequiredCalls_TrueWhenTargetFileCallsAddIgniteUIBlazor()
    {
        SetupDocuments(("MauiProgram.cs", "builder.Services.AddMauiBlazorWebView();\nbuilder.Services.AddIgniteUIBlazor();"));

        Assert.True(await CreateStep("MauiProgram.cs").ContainsRequiredCallsAsync());
    }

    [Fact]
    public async Task ContainsRequiredCalls_ChecksTheDocumentThatTheModifierChanges_NotOtherSameNamedFiles()
    {
        // Same-named files that the project does not compile (an excluded sample, a nested project, build output) are not
        // changed by the code modifier, so their registrations do not count.
        SetupDocuments(
            ("Program.cs", "var builder = WebApplication.CreateBuilder(args);\nvar app = builder.Build();"),
            ("Startup.cs", "builder.Services.AddIgniteUIBlazor();"));
        SetupFileOnDisk(Path.Combine("Samples", "Program.cs"), "builder.Services.AddIgniteUIBlazor();");
        SetupFileOnDisk(Path.Combine("Client", "Program.cs"), "builder.Services.AddIgniteUIBlazor();");
        SetupFileOnDisk(Path.Combine("obj", "Debug", "Program.cs"), "builder.Services.AddIgniteUIBlazor();");

        Assert.False(await CreateStep().ContainsRequiredCallsAsync());
    }

    [Fact]
    public async Task ContainsRequiredCalls_FindsTheDocumentInASubfolder()
    {
        SetupDocuments(("Startup" + Path.DirectorySeparatorChar + "Program.cs", "builder.Services.AddIgniteUIBlazor();"));

        Assert.True(await CreateStep().ContainsRequiredCallsAsync());
    }

    [Fact]
    public async Task ContainsRequiredCalls_FalseWhenTheProjectHasNoTargetDocument()
    {
        SetupDocuments(("Startup.cs", "builder.Services.AddIgniteUIBlazor();"));
        SetupFileOnDisk("Program.cs", "builder.Services.AddIgniteUIBlazor();");

        Assert.False(await CreateStep().ContainsRequiredCallsAsync());
    }

    [Fact]
    public async Task ContainsRequiredCalls_RequiresEveryCallInTheSameFile()
    {
        SetupDocuments(("Program.cs", "builder.Services.AddRazorComponents()\n    .AddInteractiveServerComponents();\napp.MapRazorComponents<App>();"));

        Assert.False(await CreateStep(requiredCalls: ["AddInteractiveServerComponents", "AddInteractiveServerRenderMode"]).ContainsRequiredCallsAsync());
        Assert.True(await CreateStep(requiredCalls: ["AddInteractiveServerComponents"]).ContainsRequiredCallsAsync());
    }

    [Theory]
    [InlineData("var builder = WebApplication.CreateBuilder(args);\n// builder.Services.AddIgniteUIBlazor();\nvar app = builder.Build();", false)]
    [InlineData("var builder = WebApplication.CreateBuilder(args);\n/* builder.Services.AddIgniteUIBlazor(); */\nvar app = builder.Build();", false)]
    [InlineData("var builder = WebApplication.CreateBuilder(args);\nConsole.WriteLine(\"builder.Services.AddIgniteUIBlazor();\");\nvar app = builder.Build();", false)]
    [InlineData("var builder = WebApplication.CreateBuilder(args);\n#if false\nbuilder.Services.AddIgniteUIBlazor();\n#endif\nvar app = builder.Build();", false)]
    [InlineData("var builder = WebApplication.CreateBuilder(args);\nvar addIgniteUIBlazor = builder.Services.AddIgniteUIBlazor;\nvar app = builder.Build();", false)]
    [InlineData("var builder = WebApplication.CreateBuilder(args);\nbuilder.Services.AddIgniteUIBlazor ();\nvar app = builder.Build();", true)]
    [InlineData("var builder = WebApplication.CreateBuilder(args);\nbuilder.Services\n    .AddRazorComponents()\n    .Services.AddIgniteUIBlazor();\nvar app = builder.Build();", true)]
    public async Task ContainsRequiredCalls_OnlyCountsInvocationsInActiveCSharpCode(string program, bool expected)
    {
        SetupDocuments(("Program.cs", program));

        Assert.Equal(expected, await CreateStep().ContainsRequiredCallsAsync());
    }

    [Theory]
    [InlineData(new string[] { "NET8_0_OR_GREATER" }, true)]
    [InlineData(new string[0], false)]
    public async Task ContainsRequiredCalls_UsesTheProjectsPreprocessorSymbols(string[] preprocessorSymbols, bool expected)
    {
        SetupDocuments(new CSharpParseOptions(preprocessorSymbols: preprocessorSymbols),
            ("Program.cs", "var builder = WebApplication.CreateBuilder(args);\n#if NET8_0_OR_GREATER\nbuilder.Services.AddIgniteUIBlazor();\n#endif\nvar app = builder.Build();"));

        Assert.Equal(expected, await CreateStep().ContainsRequiredCallsAsync());
    }

    [Fact]
    public async Task ExecuteAsync_ReportsIncompleteSetup_WhenRegistrationCannotBeAdded()
    {
        // The shared code modification step fails here (no config), and the registration is not present.
        SetupDocuments(("Program.cs", "var app = WebApplication.Create(args);"));
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
    public async Task ContainsRequiredCalls_ChecksTheExactTargetFile(string hostPage, bool expected)
    {
        var hostPagePath = Path.Combine(s_projectDirectory, "Components", "App.razor");
        _fileSystem.Setup(fs => fs.FileExists(hostPagePath)).Returns(true);
        _fileSystem.Setup(fs => fs.ReadAllText(hostPagePath)).Returns(hostPage);
        var step = CreateStep("App.razor", requiredCalls: ["_content/IgniteUI.Blazor/themes/dark/material.css"]);
        step.TargetFilePath = hostPagePath;

        Assert.Equal(expected, await step.ContainsRequiredCallsAsync());
        _codeService.Verify(codeService => codeService.GetWorkspaceAsync(), Times.Never);
    }

    [Theory]
    [InlineData("App.razor", "<head><!-- <link href=\"_content/IgniteUI.Blazor/themes/dark/material.css\" rel=\"stylesheet\" /> --></head>", true, false)]
    [InlineData("App.razor", "<head>@*\n<link href=\"_content/IgniteUI.Blazor/themes/dark/material.css\" rel=\"stylesheet\" />\n*@</head>", true, false)]
    [InlineData("App.razor", "<head><!-- old --><link href=\"_content/IgniteUI.Blazor/themes/dark/material.css\" rel=\"stylesheet\" /></head>", true, true)]
    [InlineData("App.razor", "<head><!-- <link href=\"_content/IgniteUI.Blazor/themes/dark/material.css\" rel=\"stylesheet\" /> --></head>", false, true)]
    public async Task ContainsRequiredCalls_IgnoresCommentedOutMarkup_WhenRequested(string fileName, string hostPage, bool ignoreMarkupComments, bool expected)
    {
        var hostPagePath = Path.Combine(s_projectDirectory, "Components", fileName);
        _fileSystem.Setup(fs => fs.FileExists(hostPagePath)).Returns(true);
        _fileSystem.Setup(fs => fs.ReadAllText(hostPagePath)).Returns(hostPage);
        var step = CreateStep(fileName, requiredCalls: ["_content/IgniteUI.Blazor/themes/dark/material.css"]);
        step.TargetFilePath = hostPagePath;
        step.IgnoreMarkupComments = ignoreMarkupComments;

        Assert.Equal(expected, await step.ContainsRequiredCallsAsync());
    }
}
