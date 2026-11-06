// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.DotNet.Scaffolding.Core.Model;
using Microsoft.DotNet.Scaffolding.Core.Scaffolders;
using Microsoft.DotNet.Scaffolding.Core.Steps;
using Microsoft.DotNet.Scaffolding.Internal.Services;
using Microsoft.DotNet.Scaffolding.Internal.Telemetry;
using Microsoft.DotNet.Scaffolding.TextTemplating.DbContext;
using Microsoft.DotNet.Tools.Scaffold.AspNet.Common;
using Microsoft.DotNet.Tools.Scaffold.AspNet.Helpers;
using Microsoft.DotNet.Tools.Scaffold.AspNet.Models;
using Microsoft.DotNet.Tools.Scaffold.AspNet.ScaffoldSteps.Settings;
using Microsoft.DotNet.Tools.Scaffold.AspNet.Telemetry;
using Microsoft.Extensions.Logging;
using AspNetConstants = Microsoft.DotNet.Tools.Scaffold.AspNet.Common.Constants;
using Constants = Microsoft.DotNet.Scaffolding.Internal.Constants;

namespace Microsoft.DotNet.Tools.Scaffold.AspNet.ScaffoldSteps;

/// <summary>
/// Scaffold step to validate Blazor CRUD settings and initialize the BlazorCrudModel for scaffolding.
/// </summary>
internal class ValidateBlazorCrudStep : ScaffoldStep
{
    private readonly IFileSystem _fileSystem;
    private readonly ILogger _logger;
    private readonly ITelemetryService _telemetryService;

    /// <summary>
    /// Path to the project file.
    /// </summary>
    public string? Project { get; set; }
    /// <summary>
    /// Indicates if prerelease packages should be used.
    /// </summary>
    public bool Prerelease { get; set; }
    /// <summary>
    /// Database provider for the DbContext.
    /// </summary>
    public string? DatabaseProvider { get; set; }
    /// <summary>
    /// Name of the DbContext class.
    /// </summary>
    public string? DataContext { get; set; }
    /// <summary>
    /// Name of the model class for CRUD operations.
    /// </summary>
    public string? Model { get; set; }
    /// <summary>
    /// Page type for CRUD scaffolding.
    /// </summary>
    public string? Page { get; set; }

    /// <summary>
    /// Constructor for ValidateBlazorCrudStep.
    /// </summary>
    /// <param name="fileSystem">The file system object.</param>
    /// <param name="logger">The logger object.</param>
    /// <param name="telemetryService">The telemetry service object.</param>
    public ValidateBlazorCrudStep(
        IFileSystem fileSystem,
        ILogger<ValidateBlazorCrudStep> logger,
        ITelemetryService telemetryService)
    {
        _fileSystem = fileSystem;
        _logger = logger;
        _telemetryService = telemetryService;
    }

    /// <summary>
    /// Executes the step to validate Blazor CRUD settings and initialize the BlazorCrudModel.
    /// </summary>
    /// <param name="context">The scaffolder context.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>A task representing the asynchronous operation, with a result indicating success or failure.</returns>
    public override async Task<bool> ExecuteAsync(ScaffolderContext context, CancellationToken cancellationToken = default)
    {
        var blazorCrudSettings = ValidateBlazorCrudSettings();
        var codeModifierProperties = new Dictionary<string, string>();
        if (blazorCrudSettings is null)
        {
            _telemetryService.TrackEvent(new ValidateScaffolderTelemetryEvent(nameof(ValidateBlazorCrudStep), context.Scaffolder.DisplayName, result: false));
            return false;
        }
        else
        {
            context.Properties.Add(nameof(CrudSettings), blazorCrudSettings);
        }

        //initialize MinimalApiModel
        _logger.LogInformation("Initializing scaffolding model...");
        var blazorCrudModel = await GetBlazorCrudModelAsync(context, blazorCrudSettings);
        if (blazorCrudModel is null)
        {
            _logger.LogError("An error occurred.");
            _telemetryService.TrackEvent(new ValidateScaffolderTelemetryEvent(nameof(ValidateBlazorCrudStep), context.Scaffolder.DisplayName, result: false));
            return false;
        }
        else
        {
            context.Properties.Add(nameof(BlazorCrudModel), blazorCrudModel);
        }

        //Install packages and add a DbContext (if needed)
        if (blazorCrudModel.DbContextInfo.EfScenario)
        {
            var dbContextProperties = AspNetDbContextHelper.GetDbContextProperties(blazorCrudSettings.Project, blazorCrudModel.DbContextInfo);
            if (dbContextProperties is not null)
            {
                context.Properties.Add(nameof(DbContextProperties), dbContextProperties);
            }

            var projectBasePath = Path.GetDirectoryName(blazorCrudSettings.Project);
            if (!string.IsNullOrEmpty(projectBasePath))
            {
                context.Properties.Add(Constants.StepConstants.BaseProjectPath, projectBasePath);
            }

            codeModifierProperties = AspNetDbContextHelper.GetDbContextCodeModifierProperties(blazorCrudModel.DbContextInfo);
        }

        if (blazorCrudModel.ProjectInfo.LowestSupportedTargetFramework.IsNetVersionOrLater(9))
        {
            var appPath = Path.Combine(Path.GetDirectoryName(blazorCrudSettings.Project)!, "Components", "App.razor");
            if (_fileSystem.FileExists(appPath))
            {
                var appContent = _fileSystem.ReadAllText(appPath);
                var routes = GetComponentRenderMode(appContent, "Routes");
                var expression = GetRenderModeExpression(routes.Expression);
                var existingProperty = expression.Trim('(', ')', ' ', '\r', '\n', '\t');
                if (!string.IsNullOrEmpty(routes.Attribute) && !Regex.IsMatch(appContent,
                    $@"\b{Regex.Escape(existingProperty)}\s*=>\s*\w+\.AcceptsInteractiveRouting\(\)", RegexOptions.None, TimeSpan.FromSeconds(1)))
                {
                    blazorCrudModel.ProjectInfo.CodeChangeOptions!.Add("GlobalInteractive");
                    var httpContextName = GetUniqueMemberName("HttpContext", appContent);
                    var pageRenderModeName = GetUniqueMemberName("PageRenderMode", appContent);
                    var members = $$"""
                            [CascadingParameter]
                            private HttpContext {{httpContextName}} { get; set; } = default!;

                            private IComponentRenderMode? {{pageRenderModeName}} =>
                                {{httpContextName}}.AcceptsInteractiveRouting() ? {{expression}} : null;
                        """;
                    codeModifierProperties.Add("$(RoutesRenderModeTag)", routes.Tag);
                    codeModifierProperties.Add("$(UpdatedRoutesRenderModeTag)",
                        routes.Tag.Replace(routes.Attribute, $"@rendermode=\"{pageRenderModeName}\""));

                    var headOutlet = GetComponentRenderMode(appContent, "HeadOutlet");
                    var updatedHeadOutlet = headOutlet.Tag;
                    if (!string.IsNullOrEmpty(headOutlet.Attribute))
                    {
                        var headOutletExpression = GetRenderModeExpression(headOutlet.Expression);
                        var headOutletRenderModeName = pageRenderModeName;
                        if (headOutletExpression != expression)
                        {
                            headOutletRenderModeName = GetUniqueMemberName("HeadOutletRenderMode", appContent);
                            members += $"""


                                    private IComponentRenderMode? {headOutletRenderModeName} =>
                                        {httpContextName}.AcceptsInteractiveRouting() ? {headOutletExpression} : null;
                                """;
                        }

                        updatedHeadOutlet = headOutlet.Tag.Replace(headOutlet.Attribute, $"@rendermode=\"{headOutletRenderModeName}\"");
                    }

                    codeModifierProperties.Add("$(HeadOutletRenderModeTag)", headOutlet.Tag);
                    codeModifierProperties.Add("$(UpdatedHeadOutletRenderModeTag)", updatedHeadOutlet);
                    codeModifierProperties.Add("$(BlazorCrudRenderModeMembers)", members);
                }
            }
        }

        context.Properties.Add(Constants.StepConstants.CodeModifierProperties, codeModifierProperties);
        var additionalCodeChanges = await BlazorCrudHelper.GetBlazorCrudCodeChangesAsync(blazorCrudModel);
        if (additionalCodeChanges.Count != 0)
        {
            var allCodeChangesString = string.Join(",", additionalCodeChanges);
            var codeModificationConfigString = BlazorCrudHelper.AdditionalCodeModificationJson.Replace("$(CodeChanges)", allCodeChangesString);
            context.Properties.Add(Constants.StepConstants.AdditionalCodeModifier, codeModificationConfigString);
        }

        _telemetryService.TrackEvent(new ValidateScaffolderTelemetryEvent(nameof(ValidateBlazorCrudStep), context.Scaffolder.DisplayName, result: true));
        return true;
    }

    // Ignore comments and mask explicit Razor expressions without changing their original offsets.
    private static (string Tag, string Attribute, string Expression) GetComponentRenderMode(string content, string componentName)
    {
        var searchableContent = MarkupHelper.MaskComments(content, htmlComments: true, razorComments: true);
        var opening = Regex.Match(searchableContent, $@"<{Regex.Escape(componentName)}\b", RegexOptions.None, TimeSpan.FromSeconds(1));
        if (!opening.Success)
        {
            return (string.Empty, string.Empty, string.Empty);
        }

        var tag = content[opening.Index..];
        var masked = tag.ToCharArray();
        var quote = '\0';
        for (var index = opening.Length; index < tag.Length; index++)
        {
            var character = tag[index];
            if (quote == '\0' && character == '>')
            {
                const string pattern = """@rendermode\s*=\s*(?:"(?<expression>[^"]+)"|'(?<expression>[^']+)')""";
                var attribute = Regex.Match(new string(masked, 0, index + 1), pattern, RegexOptions.None, TimeSpan.FromSeconds(1));
                var value = attribute.Groups["expression"];
                return (tag[..(index + 1)], tag.Substring(attribute.Index, attribute.Length), tag.Substring(value.Index, value.Length));
            }

            if (character == quote || (quote == '\0' && character is '"' or '\''))
            {
                quote = quote == '\0' ? character : '\0';
            }
            else if (quote != '\0' && character == '@' && index + 1 < tag.Length)
            {
                if (tag[index + 1] == '@')
                {
                    index++;
                    continue;
                }

                if (tag[index + 1] != '(')
                {
                    continue;
                }

                var depth = 0;
                var end = index;
                foreach (var token in SyntaxFactory.ParseTokens(tag[(index + 1)..]))
                {
                    depth += token.Kind() switch { SyntaxKind.OpenParenToken => 1, SyntaxKind.CloseParenToken => -1, _ => 0 };
                    if (depth == 0)
                    {
                        end = index + 1 + token.Span.End;
                        break;
                    }
                }

                if (end == index)
                {
                    throw new InvalidOperationException("Unable to parse an explicit Razor expression in the component markup.");
                }

                Array.Fill(masked, ' ', index, end - index);
                index = end - 1;
            }
        }

        return (string.Empty, string.Empty, string.Empty);
    }

    private static string GetRenderModeExpression(string value)
    {
        var expression = value.Trim();
        return expression.StartsWith("@(", StringComparison.Ordinal) && expression.EndsWith(')')
            ? expression[2..^1].Trim()
            : expression.TrimStart('@');
    }

    private static string GetUniqueMemberName(string name, string content)
    {
        var candidate = name;
        for (var suffix = 1; Regex.IsMatch(content, $@"\b{candidate}\b", RegexOptions.None, TimeSpan.FromSeconds(1)); suffix++)
        {
            candidate = name + suffix;
        }

        return candidate;
    }

    /// <summary>
    /// Validates the Blazor CRUD settings provided by the user.
    /// </summary>
    /// <returns>A CrudSettings object containing the validated settings, or null if validation fails.</returns>
    private CrudSettings? ValidateBlazorCrudSettings()
    {
        if (string.IsNullOrEmpty(Project) || !_fileSystem.FileExists(Project))
        {
            _logger.LogError($"Missing/Invalid {AspNetConstants.CliOptions.ProjectCliOption} option.");
            return null;
        }

        if (string.IsNullOrEmpty(Model))
        {
            _logger.LogError($"Missing/Invalid {AspNetConstants.CliOptions.ModelCliOption} option.");
            return null;
        }

        if (string.IsNullOrEmpty(Page))
        {
            _logger.LogError($"Missing/Invalid {AspNetConstants.CliOptions.PageTypeOption} option.");
            return null;
        }
        else if (!string.IsNullOrEmpty(Page) && !BlazorCrudHelper.CRUDPages.Contains(Page, StringComparer.OrdinalIgnoreCase))
        {
            //if an invalid page name, switch to just "CRUD" and scaffold all pages
            Page = BlazorCrudHelper.CrudPageType;
        }

        if (string.IsNullOrEmpty(DataContext))
        {
            _logger.LogError($"Missing/Invalid {AspNetConstants.CliOptions.DataContextOption} option.");
            return null;
        }
        else
        {
            if (!SyntaxFacts.IsValidIdentifier(DataContext) || DataContext.Equals("DbContext", StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogInformation($"Invalid {AspNetConstants.CliOptions.DataContextOption} option");
                _logger.LogInformation($"Using default '{AspNetConstants.NewDbContext}'");
                DataContext = AspNetConstants.NewDbContext;
            }

            if (string.IsNullOrEmpty(DatabaseProvider) || !PackageConstants.EfConstants.EfPackagesDict.ContainsKey(DatabaseProvider))
            {
                DatabaseProvider = PackageConstants.EfConstants.SqlServer;
            }
        }

        return new CrudSettings
        {
            Model = Model,
            Project = Project,
            Page = Page,
            DataContext = DataContext,
            DatabaseProvider = DatabaseProvider,
            Prerelease = Prerelease,
        };
    }

    /// <summary>
    /// Initializes and returns the BlazorCrudModel for scaffolding.
    /// </summary>
    /// <param name="context">The scaffolder context.</param>
    /// <param name="settings">The CRUD settings containing user input.</param>
    /// <returns>A task representing the asynchronous operation, with a result containing the BlazorCrudModel, or null if initialization fails.</returns>
    private async Task<BlazorCrudModel?> GetBlazorCrudModelAsync(ScaffolderContext context, CrudSettings settings)
    {
        ProjectInfo projectInfo = ClassAnalyzers.GetProjectInfo(settings.Project, _logger);
        context.SetSpecifiedTargetFramework(projectInfo.LowestSupportedTargetFramework);
        if (projectInfo is null || projectInfo.CodeService is null)
        {
            return null;
        }

        bool hasMainLayout = false;
        if (!string.IsNullOrEmpty(settings.Project))
        {
            var projectDirectory = Path.GetDirectoryName(settings.Project);
            if (!string.IsNullOrEmpty(projectDirectory) && _fileSystem.DirectoryExists(projectDirectory))
            {
                hasMainLayout = _fileSystem.EnumerateFiles(projectDirectory, "MainLayout.razor", SearchOption.AllDirectories).Any();
            }
        }

        //find and set --model class properties
        ModelInfo? modelInfo = null;
        var allClasses = await projectInfo.CodeService.GetAllClassSymbolsAsync();
        var modelClassSymbol = allClasses.FirstOrDefault(x => x.Name.Equals(settings.Model, StringComparison.OrdinalIgnoreCase));
        if (string.IsNullOrEmpty(settings.Model) || modelClassSymbol is null)
        {
            _logger.LogError($"Invalid {AspNetConstants.CliOptions.ModelCliOption} '{settings.Model}'");
            return null;
        }
        else
        {
            modelInfo = ClassAnalyzers.GetModelClassInfo(modelClassSymbol);
        }

        var validateModelInfoResult = ClassAnalyzers.ValidateModelForCrudScaffolders(modelInfo, _logger);
        if (!validateModelInfoResult)
        {
            _logger.LogError($"Invalid {AspNetConstants.CliOptions.ModelCliOption} '{settings.Model}'");
            return null;
        }

        //find DbContext info or create properties for a new one.
        var dbContextClassName = settings.DataContext;
        DbContextInfo dbContextInfo = new();

        if (!string.IsNullOrEmpty(dbContextClassName) && !string.IsNullOrEmpty(settings.DatabaseProvider))
        {
            var dbContextClassSymbol = allClasses.FirstOrDefault(x => x.Name.Equals(dbContextClassName, StringComparison.OrdinalIgnoreCase));
            dbContextInfo = ClassAnalyzers.GetDbContextInfo(settings.Project, dbContextClassSymbol, dbContextClassName, settings.DatabaseProvider, modelInfo);
            dbContextInfo.EfScenario = true;
        }

        BlazorCrudModel scaffoldingModel = new()
        {
            PageType = settings.Page,
            ProjectInfo = projectInfo,
            ModelInfo = modelInfo,
            DbContextInfo = dbContextInfo,
            HasMainLayout = hasMainLayout
        };

        if (scaffoldingModel.ProjectInfo is not null && scaffoldingModel.ProjectInfo.CodeService is not null)
        {
            scaffoldingModel.ProjectInfo.CodeChangeOptions =
            [
                scaffoldingModel.DbContextInfo.EfScenario ? "EfScenario" : string.Empty
            ];
        }

        return scaffoldingModel;
    }
}
