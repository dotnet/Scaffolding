// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using System.IO;
using Microsoft.CodeAnalysis;
using Microsoft.DotNet.Scaffolding.Roslyn;
using Microsoft.DotNet.Scaffolding.TextTemplating;
using Microsoft.DotNet.Tools.Scaffold.AspNet.Models;
using Microsoft.DotNet.Tools.Scaffold.AspNet.Templates.net10.BlazorCrud;

namespace Microsoft.DotNet.Tools.Scaffold.AspNet.Helpers;

/// <summary>
/// Helper methods for Blazor CRUD scaffolding, including template and code generation utilities.
/// </summary>
internal static class BlazorCrudHelper
{
    /// <summary>
    /// The CRUD page type.
    /// </summary>
    internal static string CrudPageType = "CRUD";
    internal static List<string> CRUDPages = [CrudPageType, "Create", "Delete", "Details", "Edit", "Index", "NotFound"];
    internal const string CreateBlazorTemplate = "Create.tt";

    /// <summary>
    /// The file name of the delete Blazor template.
    /// </summary>
    internal const string DeleteBlazorTemplate = "Delete.tt";

    /// <summary>
    /// The file name of the details Blazor template.
    /// </summary>
    internal const string DetailsBlazorTemplate = "Details.tt";

    /// <summary>
    /// The file name of the edit Blazor template.
    /// </summary>
    internal const string EditBlazorTemplate = "Edit.tt";

    /// <summary>
    /// The file name of the index Blazor template.
    /// </summary>
    internal const string IndexBlazorTemplate = "Index.tt";
    internal const string NotFoundBlazorTemplate = "NotFound.tt";
    internal const string IEndpointRouteBuilderContainingType = "Microsoft.AspNetCore.Routing.IEndpointRouteBuilder";

    /// <summary>
    /// The type name for IRazorComponentsBuilder, used in Blazor server-side configuration.
    /// </summary>
    internal const string IRazorComponentsBuilderType = "Microsoft.Extensions.DependencyInjection.IRazorComponentsBuilder";

    /// <summary>
    /// The type name for IServiceCollection, used for dependency injection in Blazor applications.
    /// </summary>
    internal const string IServiceCollectionType = "Microsoft.Extensions.DependencyInjection.IServiceCollection";

    /// <summary>
    /// The type name for RazorComponentsEndpointConventionBuilder, used in endpoint routing for Blazor components.
    /// </summary>
    internal const string RazorComponentsEndpointsConventionBuilderType = "Microsoft.AspNetCore.Builder.RazorComponentsEndpointConventionBuilder";

    /// <summary>
    /// The type name for IServerSideBlazorBuilder, used in Blazor server-side setup.
    /// </summary>
    internal const string IServerSideBlazorBuilderType = "Microsoft.Extensions.DependencyInjection.IServerSideBlazorBuilder";

    /// <summary>
    /// Method name for adding interactive WebAssembly components.
    /// </summary>
    internal const string AddInteractiveWebAssemblyComponentsMethod = "AddInteractiveWebAssemblyComponents";

    /// <summary>
    /// Method name for adding interactive server components.
    /// </summary>
    internal const string AddInteractiveServerComponentsMethod = "AddInteractiveServerComponents";

    /// <summary>
    /// Method name for configuring interactive WebAssembly render mode.
    /// </summary>
    internal const string AddInteractiveWebAssemblyRenderModeMethod = "AddInteractiveWebAssemblyRenderMode";

    /// <summary>
    /// Method name for configuring interactive server render mode.
    /// </summary>
    internal const string AddInteractiveServerRenderModeMethod = "AddInteractiveServerRenderMode";

    /// <summary>
    /// Method name for adding Razor components.
    /// </summary>
    internal const string AddRazorComponentsMethod = "AddRazorComponents";

    /// <summary>
    /// Method name for mapping Razor components.
    /// </summary>
    internal const string MapRazorComponentsMethod = "MapRazorComponents";

    /// <summary>
    /// Text representing the global server render mode configuration.
    /// </summary>
    internal const string GlobalServerRenderModeText = @"<HeadOutlet @rendermode=""@InteractiveServer"" />";

    /// <summary>
    /// Text representing the global WebAssembly render mode configuration.
    /// </summary>
    internal const string GlobalWebAssemblyRenderModeText = @"<HeadOutlet @rendermode=""@InteractiveWebAssembly"" />";

    /// <summary>
    /// Text representing the global WebAssembly render mode routes configuration.
    /// </summary>
    internal const string GlobalWebAssemblyRenderModeRoutesText = @"<Routes @rendermode=""@InteractiveWebAssembly"" />";

    /// <summary>
    /// Text representing the global server render mode routes configuration.
    /// </summary>
    internal const string GlobalServerRenderModeRoutesText = @"<Routes @rendermode=""@InteractiveServer"" />";

    /// <summary>
    /// JSON template for additional code modifications during scaffolding.
    /// </summary>
    internal const string AdditionalCodeModificationJson = @"
    {
        ""Files"": [
            {
                ""FileName"": ""Program.cs"",
                ""Options"": [],
                ""Methods"": {
                    ""Global"": {
                        ""CodeChanges"": [
                            $(CodeChanges)
                        ]
                    }
                }
            }
        ]
    }";

    /// <summary>
    /// Snippet for adding Razor components service registration.
    /// </summary>
    internal const string AddRazorComponentsSnippet = @"
    {
        ""Block"": ""WebApplication.CreateBuilder.Services.AddRazorComponents()"",
        ""InsertBefore"": [
            ""var app = WebApplication.CreateBuilder.Build()""
        ],
        ""CodeChangeType"": ""Default"",
        ""LeadingTrivia"": {
            ""Newline"": true,
            ""NumberOfSpaces"": 0
        }
    }";

    /// <summary>
    /// Snippet for mapping Razor components to the application's request pipeline.
    /// </summary>
    internal const string AddMapRazorComponentsSnippet = @"
    {
        ""Block"": ""app.MapRazorComponents<App>()"",
        ""InsertBefore"": [
            ""app.Run()""
        ],
        ""CodeChangeType"": ""Default"",
        ""LeadingTrivia"": {
            ""Newline"": true,
            ""NumberOfSpaces"": 0
        }
    }";

    /// <summary>
    /// Snippet for adding interactive server render mode to the endpoint routing.
    /// </summary>
    internal const string AddInteractiveServerRenderModeSnippet = @"
    {
        ""Block"": ""AddInteractiveServerRenderMode()"",
        ""Parent"": ""MapRazorComponents<App>"",
        ""CodeChangeType"": ""MemberAccess"",
        ""LeadingTrivia"": {
            ""Newline"": true,
            ""NumberOfSpaces"": 4
        }
    }";

    /// <summary>
    /// Snippet for adding interactive server components to the service collection.
    /// </summary>
    internal const string AddInteractiveServerComponentsSnippet = @"
    {
        ""Block"": ""AddInteractiveServerComponents()"",
        ""Parent"": ""WebApplication.CreateBuilder.Services.AddRazorComponents()"",
        ""CodeChangeType"": ""MemberAccess"",
        ""LeadingTrivia"": {
            ""Newline"": true,
            ""NumberOfSpaces"": 4
        }
    }";

    /// <summary>
    /// Snippet for adding interactive WebAssembly render mode to the endpoint routing.
    /// </summary>
    internal const string AddInteractiveWebAssemblyRenderModeSnippet = @"
    {
        ""Block"": ""AddInteractiveWebAssemblyRenderMode()"",
        ""Parent"": ""MapRazorComponents<App>"",
        ""CodeChangeType"": ""MemberAccess"",
        ""LeadingTrivia"": {
            ""Newline"": true,
            ""NumberOfSpaces"": 4
        }
    }";

    /// <summary>
    /// Gets the template type for a given template path.
    /// </summary>
    /// <param name="templatePath">The path to the template file.</param>
    /// <returns>The type of the template, or null if the template path is invalid.</returns>
    internal static Type? GetTemplateType(string? templatePath)
    {
        if (string.IsNullOrEmpty(templatePath))
        {
            return null;
        }

        Type? templateType = null;

        switch (Path.GetFileName(templatePath))
        {
            case CreateBlazorTemplate:
                templateType = typeof(Create);
                break;
            case IndexBlazorTemplate:
                templateType = typeof(Templates.net10.BlazorCrud.Index);
                break;
            case DeleteBlazorTemplate:
                templateType = typeof(Delete);
                break;
            case EditBlazorTemplate:
                templateType = typeof(Edit);
                break;
            case DetailsBlazorTemplate:
                templateType = typeof(Details);
                break;
            case NotFoundBlazorTemplate:
                templateType = typeof(NotFound);
                break;
        }

        return templateType;
    }

    /// <summary>
    /// Determines if the specified template is valid based on its type and file name.
    /// </summary>
    /// <param name="templateType">The type of the template.</param>
    /// <param name="templateFileName">The file name of the template.</param>
    /// <returns>True if the template is valid; otherwise, false.</returns>
    internal static bool IsValidTemplate(string templateType, string templateFileName)
    {
        if (templateType.Equals("CRUD", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        return templateType.Equals(templateFileName, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Gets the base output path for generated files based on the model name and project path.
    /// </summary>
    /// <param name="modelName">The name of the model.</param>
    /// <param name="projectPath">The path of the project.</param>
    /// <returns>The base output path for the generated files.</returns>
    internal static string GetBaseOutputPath(string modelName, string? projectPath)
    {
        string projectBasePath = Path.GetDirectoryName(projectPath) ?? Directory.GetCurrentDirectory();
        return Path.Combine(projectBasePath, "Components", "Pages", $"{modelName}Pages");
    }

    /// <summary>
    /// Asynchronously gets the list of code changes required for Blazor CRUD components.
    /// </summary>
    /// <param name="blazorCrudModel">The Blazor CRUD model containing project and component information.</param>
    /// <returns>A task that represents the asynchronous operation, with a list of code changes as the result.</returns>
    internal static async Task<IList<string>> GetBlazorCrudCodeChangesAsync(BlazorCrudModel blazorCrudModel)
    {
        var codeChanges = new List<string>();
        var codeService = blazorCrudModel.ProjectInfo?.CodeService;
        if (codeService is not null)
        {
            var programCsDocument = await codeService.GetDocumentAsync("Program.cs");
            var appRazorDocument = await codeService.GetDocumentAsync("App.razor");
            var blazorAppProperties = await GetBlazorPropertiesAsync(programCsDocument, appRazorDocument);
            return GetAdditionalBlazorCrudCodeChanges(blazorAppProperties);
        }

        return codeChanges;
    }

    private static async Task<BlazorCrudAppProperties> GetBlazorPropertiesAsync(Document? programDocument, Document? appRazorDocument)
    {
        var blazorAppProperties = new BlazorCrudAppProperties();
        if (programDocument is not null)
        {
            blazorAppProperties.AddRazorComponentsExists = await RoslynUtilities.CheckDocumentForMethodInvocationAsync(programDocument, AddRazorComponentsMethod, IServiceCollectionType);
            if (blazorAppProperties.AddRazorComponentsExists)
            {
                blazorAppProperties.InteractiveServerComponentsExists = await RoslynUtilities.CheckDocumentForMethodInvocationAsync(programDocument, AddInteractiveServerComponentsMethod, IRazorComponentsBuilderType);
                blazorAppProperties.InteractiveWebAssemblyComponentsExists = await RoslynUtilities.CheckDocumentForMethodInvocationAsync(programDocument, AddInteractiveWebAssemblyComponentsMethod, IRazorComponentsBuilderType);
            }

            blazorAppProperties.MapRazorComponentsExists = await RoslynUtilities.CheckDocumentForMethodInvocationAsync(programDocument, MapRazorComponentsMethod, IEndpointRouteBuilderContainingType);
            if (blazorAppProperties.MapRazorComponentsExists)
            {
                bool hasInteractiveServerRenderMode = await RoslynUtilities.CheckDocumentForMethodInvocationAsync(programDocument, AddInteractiveServerRenderModeMethod, RazorComponentsEndpointsConventionBuilderType);
                bool hasInteractiveWebAssemblyRenderMode = await RoslynUtilities.CheckDocumentForMethodInvocationAsync(programDocument, AddInteractiveWebAssemblyRenderModeMethod, RazorComponentsEndpointsConventionBuilderType);

                blazorAppProperties.InteractiveServerRenderModeNeeded = !hasInteractiveServerRenderMode && !blazorAppProperties.InteractiveWebAssemblyComponentsExists;
                blazorAppProperties.InteractiveWebAssemblyRenderModeNeeded = !hasInteractiveWebAssemblyRenderMode && blazorAppProperties.InteractiveWebAssemblyComponentsExists;
            }
        }

        if (appRazorDocument != null)
        {
            blazorAppProperties.IsHeadOutletGlobal = await RoslynUtilities.CheckDocumentForTextAsync(appRazorDocument, GlobalServerRenderModeText) ||
                await RoslynUtilities.CheckDocumentForTextAsync(appRazorDocument, GlobalWebAssemblyRenderModeText);

            blazorAppProperties.AreRoutesGlobal = await RoslynUtilities.CheckDocumentForTextAsync(appRazorDocument, GlobalServerRenderModeRoutesText) ||
                await RoslynUtilities.CheckDocumentForTextAsync(appRazorDocument, GlobalWebAssemblyRenderModeRoutesText);
        }

        return blazorAppProperties;
    }

    private static IList<string> GetAdditionalBlazorCrudCodeChanges(BlazorCrudAppProperties appProperties)
    {
        var codeChanges = new List<string>();
        if (appProperties.AddRazorComponentsExists)
        {
            if (!appProperties.InteractiveWebAssemblyComponentsExists && !appProperties.InteractiveServerComponentsExists)
            {
                codeChanges.Add(AddInteractiveServerComponentsSnippet);
                codeChanges.Add(AddInteractiveServerRenderModeSnippet);
            }
        }
        else
        {
            codeChanges.Add(AddRazorComponentsSnippet);
            codeChanges.Add(AddInteractiveServerComponentsSnippet);
            codeChanges.Add(AddInteractiveServerRenderModeSnippet);
        }

        if (appProperties.MapRazorComponentsExists)
        {
            if (appProperties.InteractiveServerRenderModeNeeded)
            {
                codeChanges.Add(AddInteractiveServerRenderModeSnippet);
            }

            if (appProperties.InteractiveWebAssemblyRenderModeNeeded)
            {
                codeChanges.Add(AddInteractiveWebAssemblyRenderModeSnippet);
            }
        }
        else
        {
            codeChanges.Add(AddMapRazorComponentsSnippet);
            codeChanges.Add(AddInteractiveServerRenderModeSnippet);
        }

        return codeChanges;
    }

    /// <summary>
    /// Gets the text templating properties for the specified T4 template paths and Blazor CRUD model.
    /// </summary>
    /// <param name="allT4TemplatePaths">The collection of all T4 template paths.</param>
    /// <param name="blazorCrudModel">The Blazor CRUD model containing project and component information.</param>
    /// <returns>The collection of text templating properties for the specified templates.</returns>
    internal static IEnumerable<TextTemplatingProperty> GetTextTemplatingProperties(IEnumerable<string> allT4TemplatePaths, BlazorCrudModel blazorCrudModel)
    {
        var textTemplatingProperties = new List<TextTemplatingProperty>();
        foreach (var templatePath in allT4TemplatePaths)
        {
            var templateName = Path.GetFileNameWithoutExtension(templatePath);
            var templateType = GetTemplateType(templatePath);
            if (!string.IsNullOrEmpty(templatePath) && templateType is not null)
            {
                if (!IsValidTemplate(blazorCrudModel.PageType, templateName))
                {
                    break;
                }

                string outputFileName;
                if (string.Equals(templateName, "NotFound", StringComparison.OrdinalIgnoreCase))
                {
                    string projectBasePath = Path.GetDirectoryName(blazorCrudModel.ProjectInfo.ProjectPath) ?? Directory.GetCurrentDirectory();
                    outputFileName = Path.Combine(projectBasePath, "Components", "Pages", $"{templateName}{Common.Constants.BlazorExtension}");
                }
                else
                {
                    string baseOutputPath = GetBaseOutputPath(
                        blazorCrudModel.ModelInfo.ModelTypeName,
                        blazorCrudModel.ProjectInfo.ProjectPath);
                    outputFileName = Path.Combine(baseOutputPath, $"{templateName}{Common.Constants.BlazorExtension}");
                }

                textTemplatingProperties.Add(new()
                {
                    TemplateModel = blazorCrudModel,
                    TemplateModelName = "Model",
                    TemplatePath = templatePath,
                    TemplateType = templateType,
                    OutputPath = outputFileName
                });
            }
        }

        return textTemplatingProperties;
    }
}
