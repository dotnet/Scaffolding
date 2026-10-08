// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.DotNet.Scaffolding.CodeModification.Helpers;
using Microsoft.DotNet.Scaffolding.Roslyn.Services;
using Microsoft.DotNet.Scaffolding.Roslyn.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.DotNet.Scaffolding.CodeModification.CodeChange;

namespace Microsoft.DotNet.Scaffolding.CodeModification;

internal class ProjectModifier
{
    private readonly ILogger _consoleLogger;
    private readonly ICodeService _codeService;
    private const string Main = nameof(Main);
    private readonly StringBuilder _output;
    private readonly string _projectPath;
    private readonly CodeModifierConfig _codeModifierConfig;
    private readonly IList<string> _codeChangeOptions;

    public ProjectModifier(
        string projectPath,
        ICodeService codeService,
        ILogger consoleLogger,
        CodeModifierConfig codeModifierConfig,
        IList<string> codeChangeOptions)
    {
        _codeService = codeService;
        _consoleLogger = consoleLogger ?? throw new ArgumentNullException(nameof(consoleLogger));
        _output = new StringBuilder();
        _projectPath = projectPath;
        _codeModifierConfig = codeModifierConfig;
        _codeChangeOptions = codeChangeOptions;
    }

    public async Task<bool> RunAsync()
    {
        var target = _projectPath;
        try
        {
            if (_codeModifierConfig.Files is null || !_codeModifierConfig.Files.Any())
            {
                return false;
            }

            var solution = (await _codeService.GetWorkspaceAsync())?.CurrentSolution;
            var roslynProject = solution?.GetProject(_projectPath);
            if (roslynProject is null)
            {
                _consoleLogger.LogError($"Project '{_projectPath}' was not found in the workspace.");
                return false;
            }

            var filteredFiles = _codeModifierConfig.Files.Where(f => ProjectModifierHelper.FilterOptions(f.Options, _codeChangeOptions));
            foreach (var file in filteredFiles)
            {
                target = file.FileName ?? _projectPath;
                roslynProject = file.Extension == "html"
                    ? await HandleHtmlFileAsync(file, _codeChangeOptions, roslynProject)
                    : await HandleCodeFileAsync(file, _codeChangeOptions, roslynProject);
            }

            target = _projectPath;
            return _codeService.TryApplyChanges(roslynProject.Solution);
        }
        catch (Exception ex) when (ex is IOException or InvalidDataException or UnauthorizedAccessException or ArgumentException or NotSupportedException)
        {
            _consoleLogger.LogError(ex, "Failed to modify '{Target}': {Message}", target, ex.Message);
            return false;
        }
    }

    public string GetOutput()
    {
        return _output.ToString();
    }

    private async Task<Project> HandleCodeFileAsync(CodeFile file, IList<string> options, Project project)
    {
        switch (file.Extension)
        {
            switch (file.Extension)
            {
                case "cs":
                    // Prefer workspace document; fall back to loading from disk when
                    // MSBuildWorkspace returned an empty/partial project.
                    var document = project.GetDocument(file.FileName) ?? project.GetDocumentFromName(file.FileName);
                    document = await ModifyCsFile(file, document, options);
                    //replace simple CodeFile.Replacements
                    document = await ApplyTextReplacements(file, document, options);
                    return document?.Project ?? project;
                case "cshtml":
                    var textDoc = project.GetAdditionalDocument(file.FileName);
                    textDoc = await ModifyCshtmlFile(file, textDoc, options);
                    return textDoc?.Project ?? project;
                case "razor":
                case "html":
                    textDoc = project.GetAdditionalDocument(file.FileName);
                    if (textDoc is not null)
                    {
                        textDoc = await ApplyTextReplacements(file, textDoc, options);
                        return textDoc?.Project ?? project;
                    }

                    // Disk fallback when AdditionalDocuments were not loaded (empty MSBuild
                    // project or incomplete Adhoc fallback). Mirrors css handling.
                    ApplyDiskReplacements(project, file, options);
                    break;
                case "css":
                    ApplyDiskReplacements(project, file, options);
                    break;
                }

                ProjectModifierHelper.ApplyReplacementsOnFileOnDisk(filePathOnDisk, replacements);
                break;
        }

        return project;
    }

    private async Task<Project> HandleHtmlFileAsync(CodeFile file, IList<string> options, Project project)
    {
        var document = GetHtmlAdditionalDocument(project, file.FileName);
        if (document is null)
        {
            ModifyHtmlFileOnDisk(file, options, project);
            return project;
        }

        document = await ApplyTextReplacements(file, document, options);
        return document?.Project ?? project;
    }

    private static string NormalizePathSeparators(string path)
        => path.Replace('\\', Path.DirectorySeparatorChar).Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar);

    private static TextDocument? GetHtmlAdditionalDocument(Project project, string? fileName)
    {
        if (string.IsNullOrEmpty(fileName))
        {
            return null;
        }

        var normalizedFileName = NormalizePathSeparators(fileName);
        if (Path.GetFileName(normalizedFileName) == normalizedFileName)
        {
            return project.GetAdditionalDocument(fileName);
        }

        var filePath = GetHtmlFilePath(project, normalizedFileName);
        var comparison = OperatingSystem.IsWindows() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;
        return project.AdditionalDocuments.FirstOrDefault(document =>
            !string.IsNullOrEmpty(document.FilePath) &&
            Path.GetFullPath(NormalizePathSeparators(document.FilePath)).Equals(filePath, comparison));
    }

    private static void ModifyHtmlFileOnDisk(CodeFile file, IList<string> options, Project project)
    {
        var replacements = file.Replacements?.Where(cc => ProjectModifierHelper.FilterOptions(cc.Options, options)).ToArray();
        if (replacements is null || replacements.Length == 0)
        {
            return;
        }

        var htmlPath = GetHtmlFilePath(project, file.FileName);
        ProjectModifierHelper.ApplyReplacementsOnFileOnDisk(htmlPath, replacements);
    }

    private static string GetHtmlFilePath(Project project, string? fileName)
    {
        var projectDirectory = Path.GetDirectoryName(project.FilePath);
        if (string.IsNullOrEmpty(projectDirectory) || string.IsNullOrEmpty(fileName))
        {
            throw new InvalidDataException($"HTML file '{fileName}' requires a file name and project directory.");
        }

        projectDirectory = Path.GetFullPath(projectDirectory);
        var normalizedFileName = NormalizePathSeparators(fileName);
        if (Path.GetFileName(normalizedFileName) != normalizedFileName)
        {
            var filePath = Path.GetFullPath(normalizedFileName, projectDirectory);
            var relativePath = Path.GetRelativePath(projectDirectory, filePath);
            if (Path.IsPathRooted(relativePath) || relativePath == ".." || relativePath.StartsWith($"..{Path.DirectorySeparatorChar}", StringComparison.Ordinal))
            {
                throw new InvalidDataException($"HTML file '{fileName}' is outside the project directory.");
            }

            return filePath;
        }

        return project.GetFilesOfExtension(normalizedFileName)?.FirstOrDefault(path =>
            Path.GetFileName(path).Equals(normalizedFileName, StringComparison.OrdinalIgnoreCase) &&
            !Path.GetRelativePath(projectDirectory, path).Split(Path.DirectorySeparatorChar).Any(part =>
                part.Equals("bin", StringComparison.OrdinalIgnoreCase) || part.Equals("obj", StringComparison.OrdinalIgnoreCase)))
            ?? Path.Combine(projectDirectory, normalizedFileName);
    }

    internal static async Task<TextDocument?> ModifyCshtmlFile(CodeFile file, TextDocument? fileDoc, IList<string> options)
    {
        if (fileDoc is null || file.Methods is null || !file.Methods.TryGetValue("Global", out var globalMethod))
        {
            return fileDoc;
        }

        var filteredCodeChanges = globalMethod?.CodeChanges?.Where(cc => ProjectModifierHelper.FilterOptions(cc.Options, options));
        if (filteredCodeChanges != null && !filteredCodeChanges.Any())
        {
            return fileDoc;
        }

        // add code snippets/changes.
        return await ProjectModifierHelper.ModifyDocumentTextAsync(fileDoc, filteredCodeChanges) ?? fileDoc;
    }

    /// <summary>
    /// Updates .razor and .html files via string replacement
    /// </summary>
    /// <param name="file"></param>
    /// <param name="toolOptions"></param>
    /// <returns></returns>
    internal static async Task<T?> ApplyTextReplacements<T>(CodeFile file, T? document, IList<string> toolOptions) where T : TextDocument
    {
        if (document is null)
        {
            return null;
        }

        var replacements = file.Replacements?.Where(cc => ProjectModifierHelper.FilterOptions(cc.Options, toolOptions));
        if (replacements is null || !replacements.Any())
        {
            return document;
        }

        return await ProjectModifierHelper.ModifyDocumentTextAsync(document, replacements) ?? document;
    }

    internal async Task<Document?> ModifyCsFile(CodeFile file, Document? fileDoc, IList<string> options)
    {
        if (fileDoc is null || string.IsNullOrEmpty(fileDoc.Name))
        {
            return fileDoc;
        }

        DocumentBuilder documentBuilder = new(fileDoc, file, options, _consoleLogger);
        return await documentBuilder.RunAsync();
    }

    /// <summary>
    /// Applies filtered replacements by resolving the file on disk when the Roslyn
    /// workspace has no AdditionalDocument for it (empty MSBuild load / incomplete fallback).
    /// </summary>
    private static void ApplyDiskReplacements(Project project, CodeFile file, IList<string> options)
    {
        var filePathOnDisk = project.GetFilePath(file.FileName);
        if (string.IsNullOrEmpty(filePathOnDisk))
        {
            return;
        }

        var replacements = file.Replacements?.Where(cc => ProjectModifierHelper.FilterOptions(cc.Options, options));
        ProjectModifierHelper.ApplyReplacementsOnFileOnDisk(filePathOnDisk, replacements);
    }
}
