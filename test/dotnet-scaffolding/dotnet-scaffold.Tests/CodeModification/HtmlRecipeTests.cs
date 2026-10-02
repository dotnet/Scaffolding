// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using Microsoft.DotNet.Scaffolding.CodeModification;
using Microsoft.DotNet.Scaffolding.CodeModification.CodeChange;
using Microsoft.DotNet.Scaffolding.CodeModification.Helpers;
using Microsoft.DotNet.Scaffolding.Core.Scaffolders;
using Microsoft.DotNet.Scaffolding.Roslyn.Services;
using Microsoft.DotNet.Tools.Scaffold.Tests.Helpers;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace Microsoft.DotNet.Tools.Scaffold.Tests.CodeModification;

public class HtmlRecipeTests : IDisposable
{
    private const string Link = """    <link href="example.css" rel="stylesheet" />""";
    private readonly string _directory = Path.Combine(Path.GetTempPath(), nameof(HtmlRecipeTests), Guid.NewGuid().ToString());
    private readonly AdhocWorkspace _workspace = new();
    private readonly Mock<ICodeService> _codeService = new();
    private readonly Mock<ILogger> _logger = new();
    private readonly string _projectPath;
    private readonly string _htmlPath;

    public HtmlRecipeTests()
    {
        Directory.CreateDirectory(Path.Combine(_directory, "wwwroot"));
        _projectPath = Path.Combine(_directory, "TestProject.csproj");
        _htmlPath = Path.Combine(_directory, "wwwroot", "index.html");
        _workspace.AddProject(ProjectInfo.Create(ProjectId.CreateNewId(), VersionStamp.Create(),
            "TestProject", "TestProject", LanguageNames.CSharp, filePath: _projectPath));
        _codeService.Setup(service => service.GetWorkspaceAsync()).ReturnsAsync(_workspace);
        _codeService.Setup(service => service.TryApplyChanges(It.IsAny<Solution>()))
            .Returns((Solution solution) => _workspace.TryApplyChanges(solution));
    }

    [Fact]
    public async Task RunAsync_MissingProjectFailsAndLogs()
    {
        using var emptyWorkspace = new AdhocWorkspace();
        _codeService.Setup(service => service.GetWorkspaceAsync()).ReturnsAsync(emptyWorkspace);

        Assert.False(await CreateModifier(CreateFile()).RunAsync());
        AssertError(_projectPath);
        _codeService.Verify(service => service.TryApplyChanges(It.IsAny<Solution>()), Times.Never);
    }

    [Theory]
    [InlineData("</head>")]
    [InlineData("</HEAD>")]
    public async Task RunAsync_InsertsIntoHtmlOnDiskAndIsIdempotent(string closingHead)
    {
        File.WriteAllText(_htmlPath, $"<head>{Environment.NewLine}{closingHead}");
        Assert.Empty(_workspace.CurrentSolution.Projects.Single().AdditionalDocuments);
        var file = CreateFile();
        file.Replacements![0].CheckBlock = null;
        var modifier = CreateModifier(file);

        Assert.True(await modifier.RunAsync());
        var expected = $"<head>{Environment.NewLine}{Link}{Environment.NewLine}</head>";
        Assert.Equal(expected, File.ReadAllText(_htmlPath));
        Assert.True(await modifier.RunAsync());
        Assert.Equal(expected, File.ReadAllText(_htmlPath));
    }

    [Theory]
    [InlineData("bin", "wwwroot\\index.html", false)]
    [InlineData("bin", "index.html", false)]
    [InlineData("obj", "index.html", false)]
    [InlineData("bin", "wwwroot\\index.html", true)]
    [InlineData("obj", "wwwroot\\index.html", true)]
    [InlineData("nested", "wwwroot\\index.html", true)]
    public async Task RunAsync_MissingHostDoesNotEditOtherMatchingFiles(string directory, string fileName, bool inWorkspace)
    {
        var otherPath = Path.Combine(_directory, directory, "Debug", "net10.0", "wwwroot", "index.html");
        Directory.CreateDirectory(Path.GetDirectoryName(otherPath)!);
        File.WriteAllText(otherPath, "</head>");
        TextDocument? otherDocument = null;
        if (inWorkspace)
        {
            var project = _workspace.CurrentSolution.Projects.Single();
            otherDocument = project.AddAdditionalDocument("index.html", SourceText.From("</head>"), filePath: otherPath);
            Assert.True(_workspace.TryApplyChanges(otherDocument.Project.Solution));
        }

        var file = CreateFile();
        file.FileName = fileName;

        Assert.False(await CreateModifier(file).RunAsync());
        AssertError(fileName);
        Assert.False(File.Exists(_htmlPath));
        Assert.Equal("</head>", File.ReadAllText(otherPath));
        if (otherDocument is not null)
        {
            var unchanged = _workspace.CurrentSolution.GetAdditionalDocument(otherDocument.Id)!;
            Assert.Equal("</head>", (await unchanged.GetTextAsync()).ToString());
        }

        _codeService.Verify(service => service.TryApplyChanges(It.IsAny<Solution>()), Times.Never);
    }

    [Theory]
    [InlineData("wwwroot\\index.html")]
    [InlineData("index.html")]
    public async Task RunAsync_EditsSourceHostWithoutEditingBuildArtifacts(string fileName)
    {
        var artifactPath = Path.Combine(_directory, "bin", "Debug", "net10.0", "wwwroot", "index.html");
        Directory.CreateDirectory(Path.GetDirectoryName(artifactPath)!);
        File.WriteAllText(artifactPath, "</head>");
        File.WriteAllText(_htmlPath, "</head>");
        var file = CreateFile();
        file.FileName = fileName;

        Assert.True(await CreateModifier(file).RunAsync());
        Assert.Equal($"{Link}{Environment.NewLine}</head>", File.ReadAllText(_htmlPath));
        Assert.Equal("</head>", File.ReadAllText(artifactPath));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task RunAsync_RejectsHtmlPathOutsideProject(bool absolutePath)
    {
        var outsidePath = _directory + "-outside.html";
        File.WriteAllText(outsidePath, "</head>");
        try
        {
            var project = _workspace.CurrentSolution.Projects.Single();
            var document = project.AddAdditionalDocument("outside.html", SourceText.From("</head>"), filePath: outsidePath);
            Assert.True(_workspace.TryApplyChanges(document.Project.Solution));
            var file = CreateFile();
            file.FileName = absolutePath ? outsidePath : Path.GetRelativePath(_directory, outsidePath);

            Assert.False(await CreateModifier(file).RunAsync());
            AssertError(file.FileName);
            Assert.Equal("</head>", File.ReadAllText(outsidePath));
            var unchanged = _workspace.CurrentSolution.GetAdditionalDocument(document.Id)!;
            Assert.Equal("</head>", (await unchanged.GetTextAsync()).ToString());
        }
        finally
        {
            File.Delete(outsidePath);
        }
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task RunAsync_PrependsOrAppendsOnDiskAndIsIdempotent(bool prepend)
    {
        const string original = "<html></html>";
        const string block = "<!-- inserted -->";
        File.WriteAllText(_htmlPath, original);
        var file = CreateFile();
        file.Replacements = [new CodeSnippet { Block = block, Prepend = prepend }];
        var modifier = CreateModifier(file);
        var expected = prepend ? block + original : original + block;

        for (var run = 0; run < 2; run++)
        {
            Assert.True(await modifier.RunAsync());
            Assert.Equal(expected, File.ReadAllText(_htmlPath));
        }
    }

    [Fact]
    public void ApplyReplacementsOnFileOnDisk_PreservesLegacyCssAppendBehavior()
    {
        var cssPath = Path.Combine(_directory, "app.css");
        File.WriteAllText(cssPath, "original");

        ProjectModifierHelper.ApplyReplacementsOnFileOnDisk(cssPath,
            [new CodeSnippet { Block = "new", Prepend = true }]);

        Assert.Equal("originalnew", File.ReadAllText(cssPath));
    }

    [Fact]
    public async Task RunAsync_FiltersReplacementOptions()
    {
        File.WriteAllText(_htmlPath, "</head>");
        var file = CreateFile();
        file.Replacements =
        [
            CreateReplacement("included.css", ["enabled", "!disabled"]),
            CreateReplacement("excluded.css", ["disabled"]),
            CreateReplacement("negated.css", ["!enabled"])
        ];

        Assert.True(await CreateModifier(file, "ENABLED").RunAsync());
        var text = File.ReadAllText(_htmlPath);
        Assert.Contains("included.css", text);
        Assert.DoesNotContain("excluded.css", text);
        Assert.DoesNotContain("negated.css", text);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task RunAsync_SkipsMissingTargetWhenNoEditIsSelected(bool filterFile)
    {
        var file = CreateFile();
        if (filterFile)
        {
            file.Options = ["disabled"];
        }
        else
        {
            file.Replacements![0].Options = ["disabled"];
        }

        Assert.True(await CreateModifier(file).RunAsync());
        Assert.False(File.Exists(_htmlPath));
        Assert.Empty(_logger.Invocations);
    }

    [Fact]
    public async Task RunAsync_CheckBlockPreventsDuplicateWithoutReplacementAnchor()
    {
        const string existing = """<link href="example.css" rel="stylesheet">""";
        File.WriteAllText(_htmlPath, existing);

        Assert.True(await CreateModifier(CreateFile()).RunAsync());
        Assert.Equal(existing, File.ReadAllText(_htmlPath));
    }

    [Theory]
    [InlineData("")]
    [InlineData("<body>No head anchor</body>")]
    public async Task RunAsync_MissingReplacementAnchorFailsWithoutWriting(string content)
    {
        File.WriteAllText(_htmlPath, content);

        Assert.False(await CreateModifier(CreateFile()).RunAsync());
        Assert.Equal(content, File.ReadAllText(_htmlPath));
        AssertError("</head>");
        _codeService.Verify(service => service.TryApplyChanges(It.IsAny<Solution>()), Times.Never);
    }

    [Fact]
    public async Task RunAsync_FailedLaterReplacementDoesNotWritePartialHtml()
    {
        File.WriteAllText(_htmlPath, "</head>");
        var file = CreateFile();
        file.Replacements = [CreateReplacement(), new CodeSnippet { ReplaceSnippet = ["missing"], Block = "new" }];

        Assert.False(await CreateModifier(file).RunAsync());
        Assert.Equal("</head>", File.ReadAllText(_htmlPath));
        AssertError("missing");
    }

    [SkippableTheory]
    [InlineData(FileShare.None)]
    [InlineData(FileShare.Read)]
    public async Task RunAsync_HtmlReadOrWriteFailureFailsAndLogs(FileShare fileShare)
    {
        Skip.If(fileShare == FileShare.Read && !OperatingSystem.IsWindows(), "Denying writes while permitting reads requires Windows file sharing.");
        File.WriteAllText(_htmlPath, "</head>");
        using var lockedFile = new FileStream(_htmlPath, FileMode.Open, FileAccess.Read, fileShare);

        Assert.False(await CreateModifier(CreateFile()).RunAsync());
        AssertError("wwwroot\\index.html");
        _codeService.Verify(service => service.TryApplyChanges(It.IsAny<Solution>()), Times.Never);
    }

    [Theory]
    [InlineData("Host.html")]
    [InlineData("wwwroot\\index.html")]
    [InlineData("wwwroot/index.html")]
    public async Task RunAsync_PreservesHtmlAdditionalDocumentPath(string fileName)
    {
        var path = Path.Combine(_directory, fileName.Replace('\\', Path.DirectorySeparatorChar).Replace('/', Path.DirectorySeparatorChar));
        File.WriteAllText(path, "disk content without an anchor");
        var project = _workspace.CurrentSolution.Projects.Single();
        TextDocument? otherDocument = null;
        if (fileName != "Host.html")
        {
            var otherPath = Path.Combine(_directory, "obj", "wwwroot", "index.html");
            otherDocument = project.AddAdditionalDocument("index.html", SourceText.From("</head>"), filePath: otherPath);
            project = otherDocument.Project;
        }

        var document = project.AddAdditionalDocument("Host.html", SourceText.From("</head>"), filePath: path);
        Assert.True(_workspace.TryApplyChanges(document.Project.Solution));
        var file = CreateFile();
        file.FileName = fileName;

        Assert.True(await CreateModifier(file).RunAsync());
        var updated = _workspace.CurrentSolution.GetAdditionalDocument(document.Id)!;
        Assert.Equal($"{Link}{Environment.NewLine}</head>", (await updated.GetTextAsync()).ToString());
        Assert.Equal("disk content without an anchor", File.ReadAllText(path));
        if (otherDocument is not null)
        {
            var unchanged = _workspace.CurrentSolution.GetAdditionalDocument(otherDocument.Id)!;
            Assert.Equal("</head>", (await unchanged.GetTextAsync()).ToString());
        }
    }

    [Theory]
    [InlineData("")]
    [InlineData("unrelated content")]
    [InlineData("ANCHOR")]
    public void ApplyReplacementsOnFileOnDisk_PreservesLegacyCssNoOp(string content)
    {
        var cssPath = Path.Combine(_directory, "app.css");
        File.WriteAllText(cssPath, content);

        ProjectModifierHelper.ApplyReplacementsOnFileOnDisk(cssPath,
            [new CodeSnippet { ReplaceSnippet = ["anchor"], Block = "replacement" }]);

        Assert.Equal(content, File.ReadAllText(cssPath));
    }

    [Fact]
    [Trait("Suite", "ScaffoldIntegration")]
    public async Task ExecuteAsync_StandaloneBlazorWebAssemblyRecipeSupportsSubstitutionsAndReruns()
    {
        var create = await ScaffoldCliHelper.RunDotNetAsync(_directory,
            "new", "blazorwasm", "--name", "TestProject", "--framework", "net10.0", "--output", _directory);
        Assert.True(create.ExitCode == 0, $"dotnet new failed: {create.Output}{create.Error}");
        var build = await ScaffoldCliHelper.RunBuildAsync(_directory);
        Assert.True(build.ExitCode == 0, $"dotnet build failed: {build.Output}{build.Error}");

        var codeService = new CodeService(NullLogger.Instance, _projectPath);
        var workspace = await codeService.GetWorkspaceAsync();
        var project = Assert.Single(workspace!.CurrentSolution.Projects);
        Assert.DoesNotContain(project.AdditionalDocuments, document => document.FilePath == _htmlPath);

        var step = new CodeModificationStep(NullLogger<CodeModificationStep>.Instance)
        {
            ProjectPath = _projectPath,
            CodeChangeOptions = ["stylesheet"],
            CodeModifierConfigJsonText = """
                {
                  "Files": [{
                    "FileName": "wwwroot\\index.html",
                    "Options": ["stylesheet"],
                    "Replacements": [{
                      "ReplaceSnippet": ["{anchor}"],
                      "MultiLineBlock": ["    <link href=\"{stylesheet}\" rel=\"stylesheet\" />", "</head>"],
                      "CheckBlock": "{stylesheet}",
                      "Options": ["stylesheet"]
                    }]
                  }]
                }
                """
        };
        step.CodeModifierProperties["{stylesheet}"] = "example.css";
        step.CodeModifierProperties["{anchor}"] = "</head>";
        var context = new ScaffolderContext(Mock.Of<IScaffolder>());
        var before = File.ReadAllText(_htmlPath);

        Assert.True(await step.ExecuteAsync(context));
        var expected = before.Replace("</head>", $"{Link}{Environment.NewLine}</head>");
        Assert.Equal(expected, File.ReadAllText(_htmlPath));
        Assert.True(await step.ExecuteAsync(context));
        Assert.Equal(expected, File.ReadAllText(_htmlPath));

        step.CodeModifierConfigJsonText = step.CodeModifierConfigJsonText.Replace("index.html", "missing.html");
        Assert.False(await step.ExecuteAsync(context));
    }

    private ProjectModifier CreateModifier(CodeFile file, params string[] options)
        => new(_projectPath, _codeService.Object, _logger.Object, new CodeModifierConfig { Files = [file] }, options);

    private static CodeFile CreateFile()
        => new() { FileName = "wwwroot\\index.html", Replacements = [CreateReplacement()] };

    private static CodeSnippet CreateReplacement(string stylesheet = "example.css", string[]? options = null)
        => new()
        {
            ReplaceSnippet = ["</head>"],
            MultiLineBlock = [$"""    <link href="{stylesheet}" rel="stylesheet" />""", "</head>"],
            CheckBlock = stylesheet,
            Options = options
        };

    private void AssertError(string diagnostic)
        => Assert.Contains(_logger.Invocations, invocation =>
            invocation.Method.Name == nameof(ILogger.Log) &&
            Equals(invocation.Arguments[0], LogLevel.Error) &&
            invocation.Arguments[2].ToString()!.Contains(diagnostic));

    public void Dispose()
    {
        _workspace.Dispose();
        Directory.Delete(_directory, recursive: true);
    }
}
