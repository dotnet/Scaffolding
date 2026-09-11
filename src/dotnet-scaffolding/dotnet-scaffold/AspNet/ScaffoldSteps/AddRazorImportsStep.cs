// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using System.Text.RegularExpressions;
using Microsoft.DotNet.Scaffolding.Core.Scaffolders;
using Microsoft.DotNet.Scaffolding.Core.Steps;
using Microsoft.DotNet.Scaffolding.Internal.Services;
using Microsoft.DotNet.Scaffolding.Internal.Telemetry;
using Microsoft.DotNet.Tools.Scaffold.AspNet.Helpers;
using Microsoft.DotNet.Tools.Scaffold.AspNet.Telemetry;
using Microsoft.Extensions.Logging;

namespace Microsoft.DotNet.Tools.Scaffold.AspNet.ScaffoldSteps;

/// <summary>
/// Scaffold step that adds one or more <c>@using</c> directives to an <c>_Imports.razor</c> file.
/// Directives that are already present are left untouched; the file is created when it does not exist.
/// The edit is applied directly on disk so it works for any project layout (Blazor Web App server
/// project, Blazor WebAssembly client project, Blazor Server) and does not depend on the Roslyn
/// workspace exposing the razor file as an additional document.
/// </summary>
internal class AddRazorImportsStep : ScaffoldStep
{
    /// <summary>
    /// Gets or sets the full path of the _Imports.razor file to update (created when missing).
    /// </summary>
    public required string ImportsFilePath { get; set; }

    /// <summary>
    /// Gets or sets the namespaces to import, e.g. 'IgniteUI.Blazor.Controls'.
    /// </summary>
    public required IList<string> Namespaces { get; set; }

    private readonly ILogger _logger;
    private readonly IFileSystem _fileSystem;
    private readonly ITelemetryService _telemetryService;

    /// <summary>
    /// Initializes a new instance of the <see cref="AddRazorImportsStep"/> class.
    /// </summary>
    public AddRazorImportsStep(ILogger<AddRazorImportsStep> logger, IFileSystem fileSystem, ITelemetryService telemetryService)
    {
        _logger = logger;
        _fileSystem = fileSystem;
        _telemetryService = telemetryService;
        ContinueOnError = true;
    }

    /// <inheritdoc />
    public override Task<bool> ExecuteAsync(ScaffolderContext context, CancellationToken cancellationToken = default)
    {
        bool result = Execute();
        _telemetryService.TrackEvent(new WrappedStepTelemetryEvent(nameof(AddRazorImportsStep), context.Scaffolder.DisplayName, result));
        return Task.FromResult(result);
    }

    private bool Execute()
    {
        if (string.IsNullOrEmpty(ImportsFilePath))
        {
            _logger.LogError("No _Imports.razor path was provided.");
            return false;
        }

        var namespaces = Namespaces?.Where(ns => !string.IsNullOrWhiteSpace(ns)).Select(ns => ns.Trim()).Distinct().ToList() ?? [];
        if (namespaces.Count == 0)
        {
            return true;
        }

        var fileName = Path.GetFileName(ImportsFilePath);
        if (!_fileSystem.FileExists(ImportsFilePath))
        {
            var directory = Path.GetDirectoryName(ImportsFilePath);
            if (!string.IsNullOrEmpty(directory))
            {
                _fileSystem.CreateDirectoryIfNotExists(directory);
            }

            _logger.LogInformation($"Creating '{fileName}'...");
            var newContent = string.Join(Environment.NewLine, namespaces.Select(ns => $"@using {ns}")) + Environment.NewLine;
            _fileSystem.WriteAllText(ImportsFilePath, newContent);
            _logger.LogInformation("Done");
            return true;
        }

        var content = _fileSystem.ReadAllText(ImportsFilePath) ?? string.Empty;
        var updatedContent = AddUsings(content, namespaces, out var addedNamespaces);
        if (addedNamespaces.Count == 0)
        {
            _logger.LogInformation($"'{fileName}' already imports {string.Join(", ", namespaces)}.");
            return true;
        }

        _logger.LogInformation($"Updating '{fileName}'...");
        _fileSystem.WriteAllText(ImportsFilePath, updatedContent);
        _logger.LogInformation("Done");
        return true;
    }

    /// <summary>
    /// Appends <c>@using</c> directives for the namespaces that are not yet imported by <paramref name="content"/>.
    /// </summary>
    /// <param name="content">Existing _Imports.razor content.</param>
    /// <param name="namespaces">Namespaces that must be imported.</param>
    /// <param name="addedNamespaces">The namespaces that were appended.</param>
    /// <returns>The updated content (unchanged when nothing had to be added).</returns>
    internal static string AddUsings(string? content, IEnumerable<string> namespaces, out IList<string> addedNamespaces)
    {
        addedNamespaces = [];
        content ??= string.Empty;
        var lineEnding = IgniteUIBlazorHelper.DetectLineEnding(content);
        var updated = content;

        foreach (var ns in namespaces)
        {
            if (ContainsUsing(updated, ns))
            {
                continue;
            }

            if (updated.Length > 0 && !updated.EndsWith('\n'))
            {
                updated += lineEnding;
            }

            updated += $"@using {ns}{lineEnding}";
            addedNamespaces.Add(ns);
        }

        return updated;
    }

    /// <summary>
    /// Returns true when <paramref name="content"/> already contains an <c>@using</c> directive for <paramref name="ns"/>.
    /// </summary>
    internal static bool ContainsUsing(string? content, string ns)
    {
        if (string.IsNullOrEmpty(content))
        {
            return false;
        }

        var pattern = $@"^\s*@using\s+{Regex.Escape(ns)}\s*;?\s*$";
        return Regex.IsMatch(content, pattern, RegexOptions.Multiline);
    }
}
