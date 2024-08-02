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
            Document? originalDocument = roslynProject?.GetDocument(file.FileName);
            var modifiedDocument = await HandleCodeFileAsync(originalDocument, file, _codeChangeOptions);
            roslynProject = modifiedDocument?.Project;
        }

        return _codeService.TryApplyChanges(roslynProject?.Solution);
    }

    private async Task<Document?> HandleCodeFileAsync(Document? document, CodeFile file, IList<string> options)
    {
        try
        {
            if (!string.IsNullOrEmpty(file.AddFilePath))
            {
                return AddFile(file);
            }
            else
            {
                switch (file.Extension)
                {
                    case "cs":
                        //apply CodeFile.CodeSnippet changes
                        var doc = await ModifyCsFile(file, document, options);
                        //replace simple CodeFile.Replacements
                        return await ApplyTextReplacements(file, doc, options);
                    case "cshtml":
                        return await ModifyCshtmlFile(file, document, options);
                    case "razor":
                    case "html":
                        return await ApplyTextReplacements(file, document, options);
                }
            }
        }
        catch (Exception)
        {
            //_output.Append(string.Format(Resources.FailedToModifyCodeFile, file.FileName, e.Message));
        }

        return document;
    }

    internal static async Task<Document?> ModifyCshtmlFile(CodeFile file, Document? fileDoc, IList<string> options)
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
    /// <param name="project"></param>
    /// <param name="toolOptions"></param>
    /// <returns></returns>
    internal static async Task<Document?> ApplyTextReplacements(CodeFile file, Document? document, IList<string> toolOptions)
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

    private Document? AddFile(CodeFile file)
    {
        /*            var filePath = Path.Combine("TODOprojectpath", file.AddFilePath);
                    if (File.Exists(filePath))
                    {
                        return; // File exists, don't need to create
                    }

                    var codeFileString = string.Empty;// GetCodeFileString(file);

                    var fileDir = Path.GetDirectoryName(filePath);
                    if (!string.IsNullOrEmpty(fileDir))
                    {
                        Directory.CreateDirectory(fileDir);
                        File.WriteAllText(filePath, codeFileString);
                    }*/
        return null;
    }
}
