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
/// Scaffold step that links an Ignite UI theme stylesheet in the host page of a Blazor application
/// (Components/App.razor, Pages/_Host.cshtml, Pages/_Layout.cshtml or wwwroot/index.html).
/// <list type="bullet">
/// <item>When the requested stylesheet is already linked, nothing changes.</item>
/// <item>When a different Ignite UI theme is linked, its href is swapped so only one theme is active.</item>
/// <item>Otherwise a &lt;link&gt; element is inserted after the last existing &lt;link&gt; in &lt;head&gt;
/// (or right before &lt;/head&gt; when there is none), using <c>@Assets["..."]</c> when the .razor host page already does.</item>
/// </list>
/// The edit is applied directly on disk because wwwroot/index.html is not part of the Roslyn workspace.
/// </summary>
internal class AddIgniteUIThemeStylesheetStep : ScaffoldStep
{
    /// <summary>
    /// Gets or sets the full path of the host page that contains the &lt;head&gt; element.
    /// </summary>
    public required string HostPagePath { get; set; }

    /// <summary>
    /// Gets or sets the project-relative stylesheet path, e.g. '_content/IgniteUI.Blazor/themes/light/bootstrap.css'.
    /// </summary>
    public required string StylesheetPath { get; set; }

    private readonly ILogger _logger;
    private readonly IFileSystem _fileSystem;
    private readonly ITelemetryService _telemetryService;

    /// <summary>
    /// Initializes a new instance of the <see cref="AddIgniteUIThemeStylesheetStep"/> class.
    /// </summary>
    public AddIgniteUIThemeStylesheetStep(ILogger<AddIgniteUIThemeStylesheetStep> logger, IFileSystem fileSystem, ITelemetryService telemetryService)
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
        _telemetryService.TrackEvent(new WrappedStepTelemetryEvent(nameof(AddIgniteUIThemeStylesheetStep), context.Scaffolder.DisplayName, result));
        return Task.FromResult(result);
    }

    private bool Execute()
    {
        if (string.IsNullOrEmpty(HostPagePath) || string.IsNullOrEmpty(StylesheetPath))
        {
            _logger.LogError("Host page path or stylesheet path was not provided.");
            return false;
        }

        if (!_fileSystem.FileExists(HostPagePath))
        {
            _logger.LogError($"Host page '{HostPagePath}' was not found.");
            return false;
        }

        var fileName = Path.GetFileName(HostPagePath);
        var content = _fileSystem.ReadAllText(HostPagePath) ?? string.Empty;
        var updatedContent = ApplyStylesheetLink(HostPagePath, content, StylesheetPath, out var outcome);

        switch (outcome)
        {
            case StylesheetLinkOutcome.AlreadyLinked:
                _logger.LogInformation($"'{fileName}' already links '{StylesheetPath}'.");
                return true;
            case StylesheetLinkOutcome.NoHeadElement:
                _logger.LogWarning($"Could not find a </head> element in '{fileName}'. Add the following line to the <head> of your host page manually:");
                _logger.LogWarning($"    {IgniteUIBlazorHelper.BuildStylesheetLink(StylesheetPath, useAssetsCollection: false)}");
                return false;
            case StylesheetLinkOutcome.Replaced:
                _logger.LogInformation($"Updating the Ignite UI theme in '{fileName}' to '{StylesheetPath}'...");
                break;
            default:
                _logger.LogInformation($"Linking '{StylesheetPath}' in '{fileName}'...");
                break;
        }

        _fileSystem.WriteAllText(HostPagePath, updatedContent);
        _logger.LogInformation("Done");
        return true;
    }

    /// <summary>
    /// Describes what <see cref="ApplyStylesheetLink"/> did to the host page content.
    /// </summary>
    internal enum StylesheetLinkOutcome
    {
        /// <summary>The requested stylesheet was already linked; content unchanged.</summary>
        AlreadyLinked,
        /// <summary>An existing Ignite UI theme link was swapped for the requested stylesheet.</summary>
        Replaced,
        /// <summary>A new &lt;link&gt; element was inserted into &lt;head&gt;.</summary>
        Inserted,
        /// <summary>No &lt;/head&gt; element was found; content unchanged.</summary>
        NoHeadElement
    }

    /// <summary>
    /// Returns the host page content with the theme stylesheet linked (see class remarks for the rules).
    /// </summary>
    /// <param name="hostPagePath">Path of the host page (used to decide on <c>@Assets</c> syntax).</param>
    /// <param name="content">Current host page content.</param>
    /// <param name="stylesheetPath">Project-relative stylesheet path to link.</param>
    /// <param name="outcome">What was done to the content.</param>
    internal static string ApplyStylesheetLink(string hostPagePath, string? content, string stylesheetPath, out StylesheetLinkOutcome outcome)
    {
        content ??= string.Empty;
        if (content.Contains(stylesheetPath, StringComparison.OrdinalIgnoreCase))
        {
            outcome = StylesheetLinkOutcome.AlreadyLinked;
            return content;
        }

        // A different Ignite UI theme is linked already: swap it so the app never loads two themes.
        if (IgniteUIBlazorHelper.ThemeStylesheetRegex.IsMatch(content))
        {
            outcome = StylesheetLinkOutcome.Replaced;
            return IgniteUIBlazorHelper.ThemeStylesheetRegex.Replace(content, stylesheetPath);
        }

        var headCloseIndex = content.IndexOf("</head>", StringComparison.OrdinalIgnoreCase);
        if (headCloseIndex < 0)
        {
            outcome = StylesheetLinkOutcome.NoHeadElement;
            return content;
        }

        var lineEnding = IgniteUIBlazorHelper.DetectLineEnding(content);
        var useAssets = IgniteUIBlazorHelper.UsesAssetsCollection(hostPagePath, content);
        var link = IgniteUIBlazorHelper.BuildStylesheetLink(stylesheetPath, useAssets);
        var head = content.Substring(0, headCloseIndex);

        // Prefer inserting right after the last <link ...> element inside <head>, matching its indentation.
        // '\r?' keeps the match working for CRLF files: in .NET, '$' with Multiline only matches before '\n'.
        var lastLink = Regex.Matches(head, @"^([ \t]*)<link\b[^>]*>[ \t]*\r?$", RegexOptions.Multiline | RegexOptions.IgnoreCase)
            .Cast<Match>()
            .LastOrDefault();
        if (lastLink is not null)
        {
            var insertAt = lastLink.Index + lastLink.Length;
            // include the line break that terminates the matched <link> line (if any)
            if (insertAt < content.Length && content[insertAt] == '\r')
            {
                insertAt++;
            }

            if (insertAt < content.Length && content[insertAt] == '\n')
            {
                insertAt++;
            }

            outcome = StylesheetLinkOutcome.Inserted;
            return content.Insert(insertAt, $"{lastLink.Groups[1].Value}{link}{lineEnding}");
        }

        // No <link> yet: insert on its own line right before </head>, indented one level deeper than </head>.
        outcome = StylesheetLinkOutcome.Inserted;
        var lineStart = content.LastIndexOf('\n', headCloseIndex) + 1;
        var headPrefix = content.Substring(lineStart, headCloseIndex - lineStart);
        if (string.IsNullOrWhiteSpace(headPrefix))
        {
            // </head> starts its own line: insert a full line above it.
            return content.Insert(lineStart, $"{headPrefix}    {link}{lineEnding}");
        }

        // </head> shares a line with other markup (e.g. '<head>...</head>'): break the line before it.
        return content.Insert(headCloseIndex, $"{lineEnding}    {link}{lineEnding}");
    }
}
