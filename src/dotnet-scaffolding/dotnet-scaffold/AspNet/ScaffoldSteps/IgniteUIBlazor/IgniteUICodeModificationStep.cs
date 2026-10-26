// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.DotNet.Scaffolding.Core.Scaffolders;
using Microsoft.DotNet.Scaffolding.Internal.Services;
using Microsoft.DotNet.Scaffolding.Roslyn.Extensions;
using Microsoft.DotNet.Scaffolding.Roslyn.Services;
using Microsoft.DotNet.Tools.Scaffold.AspNet.Helpers;
using Microsoft.Extensions.Logging;

namespace Microsoft.DotNet.Tools.Scaffold.AspNet.ScaffoldSteps;

/// <summary>
/// Applies a required Ignite UI for Blazor code change with the shared code modification step, then verifies that the
/// change is present. The code modifier leaves a file unchanged when it does not recognize its code (for example a
/// Program.cs without 'var app = builder.Build();'), so without this check an app that cannot use the Ignite UI
/// components would be reported as set up.
/// </summary>
internal class IgniteUICodeModificationStep : WrappedCodeModificationStep
{
    /// <summary>
    /// Gets or sets the name of the file that is changed: 'Program.cs', or 'MauiProgram.cs' for a .NET MAUI Blazor Hybrid app.
    /// </summary>
    public required string TargetFileName { get; set; }

    /// <summary>
    /// Gets or sets the exact file that is changed, when it is known (e.g. the resolved host page). When null, the
    /// <see cref="TargetFileName"/> document that the code modifier changes in the project is checked.
    /// </summary>
    public string? TargetFilePath { get; set; }

    /// <summary>
    /// Gets or sets the code service of the project at 'ProjectPath', which finds the
    /// <see cref="TargetFileName"/> document when <see cref="TargetFilePath"/> is null. When null, one is created.
    /// </summary>
    public ICodeService? CodeService { get; set; }

    /// <summary>
    /// Gets or sets what the changed file must contain afterwards: in a C# file, the names of the methods (e.g.
    /// 'AddIgniteUIBlazor') that it must invoke in active code, so that commented-out calls and string literals do not
    /// count; in any other file, the text (e.g. a stylesheet path) that it must contain.
    /// </summary>
    public required IReadOnlyList<string> RequiredCalls { get; set; }

    /// <summary>
    /// Gets or sets whether <see cref="RequiredCalls"/> found only inside markup comments (HTML comments, and Razor
    /// comments in .razor and .cshtml files) are ignored, e.g. for a host page whose commented-out link must not count.
    /// </summary>
    public bool IgnoreMarkupComments { get; set; }

    /// <summary>
    /// Gets or sets what the change does, e.g. "'builder.Services.AddIgniteUIBlazor()'", for the incomplete-setup error.
    /// </summary>
    public required string ChangeDescription { get; set; }

    /// <summary>
    /// Gets or sets how to make the change manually when it cannot be applied automatically.
    /// </summary>
    public required string ManualInstructions { get; set; }

    /// <summary>
    /// Gets or sets why the change cannot be made at all (e.g. no host page was found). When set, the step does not
    /// modify anything and reports the incomplete setup with <see cref="ManualInstructions"/>.
    /// </summary>
    public string? IncompleteReason { get; set; }

    private readonly ILogger _logger;
    private readonly IFileSystem _fileSystem;

    /// <summary>
    /// Initializes a new instance of the <see cref="IgniteUICodeModificationStep"/> class.
    /// </summary>
    public IgniteUICodeModificationStep(ILogger<WrappedCodeModificationStep> logger, ITelemetryService telemetryService, IFileSystem fileSystem)
        : base(logger, telemetryService)
    {
        _logger = logger;
        _fileSystem = fileSystem;
    }

    /// <inheritdoc />
    public override async Task<bool> ExecuteAsync(ScaffolderContext context, CancellationToken cancellationToken = default)
    {
        if (IncompleteReason is not null)
        {
            _logger.LogError($"Ignite UI for Blazor setup is incomplete: {IncompleteReason}. {ManualInstructions}");
            return false;
        }

        if (await base.ExecuteAsync(context, cancellationToken) && await ContainsRequiredCallsAsync())
        {
            return true;
        }

        _logger.LogError(
            $"Ignite UI for Blazor setup is incomplete: {ChangeDescription} could not be added to {TargetFileName} in '{ProjectPath}'. {ManualInstructions}");
        return false;
    }

    /// <summary>
    /// Returns true when the changed file (<see cref="TargetFilePath"/>, or the <see cref="TargetFileName"/> document that
    /// the code modifier changes) contains every one of the <see cref="RequiredCalls"/>: as invocations in a C# file,
    /// otherwise as text (outside of markup comments when <see cref="IgnoreMarkupComments"/> is set).
    /// </summary>
    internal async Task<bool> ContainsRequiredCallsAsync()
    {
        var file = TargetFilePath;
        CSharpParseOptions? parseOptions = null;
        if (file is null)
        {
            (file, parseOptions) = await GetTargetDocumentAsync();
        }

        return file is not null && _fileSystem.FileExists(file) && ContainsRequiredCalls(file, parseOptions);
    }

    /// <summary>
    /// Returns the path and parse options of the <see cref="TargetFileName"/> document that the code modifier changes:
    /// the project's first document whose name or path ends with it (see ProjectModifier in
    /// Microsoft.DotNet.Scaffolding.CodeModification). A same-named file that the project does not compile, such as one
    /// in an excluded folder or a nested project, is not changed, so it must not count either. The parse options carry
    /// the project's preprocessor symbols (e.g. 'NET8_0_OR_GREATER'), so that code in their '#if' blocks counts as active.
    /// </summary>
    private async Task<(string? FilePath, CSharpParseOptions? ParseOptions)> GetTargetDocumentAsync()
    {
        CodeService ??= new CodeService(_logger, ProjectPath);
        var project = (await CodeService.GetWorkspaceAsync())?.CurrentSolution.GetProject(ProjectPath);
        var document = project?.GetDocument(TargetFileName);
        return (document?.FilePath, document?.Project.ParseOptions as CSharpParseOptions);
    }

    private bool ContainsRequiredCalls(string file, CSharpParseOptions? parseOptions)
    {
        var content = _fileSystem.ReadAllText(file);
        if (Path.GetExtension(file).Equals(".cs", StringComparison.OrdinalIgnoreCase))
        {
            var root = CSharpSyntaxTree.ParseText(content, parseOptions).GetRoot();
            return RequiredCalls.All(call => BlazorInteractivityAnalyzer.IsInvoked(root, call));
        }

        if (IgnoreMarkupComments)
        {
            content = MarkupHelper.MaskComments(file, content);
        }

        return RequiredCalls.All(call => content.Contains(call, StringComparison.Ordinal));
    }
}
