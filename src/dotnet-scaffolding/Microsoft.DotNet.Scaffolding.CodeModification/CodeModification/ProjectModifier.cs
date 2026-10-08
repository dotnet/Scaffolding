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
        if (_codeModifierConfig.Files is null || !_codeModifierConfig.Files.Any())
        {
            return false;
        }

        var solution = (await _codeService.GetWorkspaceAsync())?.CurrentSolution;
        var roslynProject = solution?.GetProject(_projectPath);

        var filteredFiles = _codeModifierConfig.Files.Where(f => ProjectModifierHelper.FilterOptions(f.Options, _codeChangeOptions));
        foreach (var file in filteredFiles)
        {
            if (roslynProject  is not null)
            {
                roslynProject = await HandleCodeFileAsync(file, _codeChangeOptions, roslynProject);
            }
        }

        return _codeService.TryApplyChanges(roslynProject?.Solution);
    }

    private async Task<Project> HandleCodeFileAsync(CodeFile file, IList<string> options, Project project)
    {
        try
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
                    // Prefer workspace AdditionalDocument when present (in-memory edit).
                    // Use exact relative path so "_Imports.razor" does not collide with
                    // "Components/_Imports.razor" (EndsWith would match either).
                    var markupDoc = project.GetAdditionalDocumentByRelativePath(file.FileName)
                                    ?? project.GetAdditionalDocument(file.FileName);
                    if (markupDoc is not null)
                    {
                        markupDoc = await ApplyTextReplacements(file, markupDoc, options);
                        project = markupDoc?.Project ?? project;
                    }

                    // MSBuildWorkspace.TryApplyChanges often does NOT write
                    // AdditionalDocuments (.razor / .html) to disk. Always apply the same
                    // replacements on disk so root _Imports.razor, Pages/_Imports.razor,
                    // App.razor, and index.html stick. This mirrors the css handling
                    // above and also covers the case where the AdditionalDocument
                    // couldn't be located at all.
                    ApplyDiskReplacements(project, file, options);
                    return project;
                case "css":
                    ApplyDiskReplacements(project, file, options);
                    break;
            }
        }
        catch (Exception e)
        {
            _consoleLogger.LogError($"Failed to modify file '{file.FileName}', {e.Message}");
        }

        return project;
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
        return await ProjectModifierHelper.ModifyDocumentTextAsync(fileDoc, filteredCodeChanges);
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

        return await ProjectModifierHelper.ModifyDocumentTextAsync(document, replacements);
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
        var filePathOnDisk = ResolveFilePathOnDisk(project, file.FileName);
        if (string.IsNullOrEmpty(filePathOnDisk) || !File.Exists(filePathOnDisk))
        {
            return;
        }

        var replacements = file.Replacements?.Where(cc => ProjectModifierHelper.FilterOptions(cc.Options, options));
        ProjectModifierHelper.ApplyReplacementsOnFileOnDisk(filePathOnDisk, replacements);
    }

    /// <summary>
    /// Resolves a project-relative file name to a concrete on-disk path. Prefers an
    /// exact relative-path match (so "_Imports.razor" does not collide with
    /// "Components/_Imports.razor") and falls back to the existing
    /// <see cref="RoslynExtensions.GetFilePath(Project, string?)"/> search for callers
    /// that pass a bare file name.
    /// </summary>
    private static string? ResolveFilePathOnDisk(Project project, string? relativeOrName)
    {
        if (string.IsNullOrEmpty(relativeOrName) || string.IsNullOrEmpty(project.FilePath))
        {
            return null;
        }

        var projectDir = Path.GetDirectoryName(project.FilePath);
        if (string.IsNullOrEmpty(projectDir))
        {
            return null;
        }

        var normalized = relativeOrName
            .Replace('/', Path.DirectorySeparatorChar)
            .Replace('\\', Path.DirectorySeparatorChar);

        // Exact relative path first (matches discovery result).
        var exact = Path.GetFullPath(Path.Combine(projectDir, normalized));
        if (File.Exists(exact))
        {
            return exact;
        }

        // Fallback: existing EndsWith search.
        return project.GetFilePath(relativeOrName);
    }
}
