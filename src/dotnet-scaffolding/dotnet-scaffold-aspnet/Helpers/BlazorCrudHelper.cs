// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using Microsoft.CodeAnalysis;
using Microsoft.DotNet.Scaffolding.Roslyn;
using Microsoft.DotNet.Scaffolding.TextTemplating;
using Microsoft.DotNet.Tools.Scaffold.AspNet.Models;
using Microsoft.DotNet.Tools.Scaffold.AspNet.Templates.BlazorCrud;

namespace Microsoft.DotNet.Tools.Scaffold.AspNet.Helpers;

internal static class BlazorCrudHelper
{
    internal static string CrudPageType = "CRUD";
    internal static List<string> CRUDPages = [CrudPageType, "Create", "Delete", "Details", "Edit", "Index"];
    internal const string CreateBlazorTemplate = "Create.tt";
    internal const string DeleteBlazorTemplate = "Delete.tt";
    internal const string DetailsBlazorTemplate = "Details.tt";
    internal const string EditBlazorTemplate = "Edit.tt";
    internal const string IndexBlazorTemplate = "Index.tt";
    internal const string IEndpointRouteBuilderContainingType = "Microsoft.AspNetCore.Routing.IEndpointRouteBuilder";
    internal const string IRazorComponentsBuilderType = "Microsoft.Extensions.DependencyInjection.IRazorComponentsBuilder";
    internal const string IServiceCollectionType = "Microsoft.Extensions.DependencyInjection.IServiceCollection";
    internal const string RazorComponentsEndpointsConventionBuilderType = "Microsoft.AspNetCore.Builder.RazorComponentsEndpointConventionBuilder";
    internal const string IServerSideBlazorBuilderType = "Microsoft.Extensions.DependencyInjection.IServerSideBlazorBuilder";
    internal const string AddInteractiveWebAssemblyComponentsMethod = "AddInteractiveWebAssemblyComponents";
    internal const string AddInteractiveServerComponentsMethod = "AddInteractiveServerComponents";
    internal const string AddInteractiveWebAssemblyRenderModeMethod = "AddInteractiveWebAssemblyRenderMode";
    internal const string AddInteractiveServerRenderModeMethod = "AddInteractiveServerRenderMode";
    internal const string AddRazorComponentsMethod = "AddRazorComponents";
    internal const string MapRazorComponentsMethod = "MapRazorComponents";
    internal const string GlobalServerRenderModeText = @"<HeadOutlet @rendermode=""@InteractiveServer"" />";
    internal const string GlobalWebAssemblyRenderModeText = @"<HeadOutlet @rendermode=""@InteractiveWebAssembly"" />";
    internal const string GlobalWebAssemblyRenderModeRoutesText = @"<Routes @rendermode=""@InteractiveWebAssembly"" />";
    internal const string GlobalServerRenderModeRoutesText = @"<Routes @rendermode=""@InteractiveServer"" />";
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
                templateType = typeof(Templates.BlazorCrud.Index);
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
        }

        return templateType;
    }

    internal static bool IsValidTemplate(string templateType, string templateFileName)
    {
        if (templateType.Equals("CRUD", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        return templateType.Equals(templateFileName, StringComparison.OrdinalIgnoreCase);
    }

    internal static string GetBaseOutputPath(string modelName, string? projectPath)
    {
        string projectBasePath = Path.GetDirectoryName(projectPath) ?? Directory.GetCurrentDirectory();
        return Path.Combine(projectBasePath, "Components", "Pages", $"{modelName}Pages");
    }

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

                string baseOutputPath = GetBaseOutputPath(
                    blazorCrudModel.ModelInfo.ModelTypeName,
                    blazorCrudModel.ProjectInfo.ProjectPath);
                string outputFileName = Path.Combine(baseOutputPath, $"{templateName}{Common.Constants.BlazorExtension}");

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
