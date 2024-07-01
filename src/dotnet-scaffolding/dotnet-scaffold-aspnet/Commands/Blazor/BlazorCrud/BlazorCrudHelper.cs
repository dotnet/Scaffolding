// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System;
using System.IO;
using Microsoft.DotNet.Scaffolding.Helpers.Roslyn;
using Microsoft.DotNet.Scaffolding.Helpers.T4Templating;
using Microsoft.DotNet.Tools.Scaffold.AspNet.Templates.BlazorCrud;
using Microsoft.DotNet.Tools.Scaffold.AspNet.Helpers;
using Microsoft.CodeAnalysis;
using System.Threading.Tasks;
using System.Linq;

namespace Microsoft.DotNet.Tools.Scaffold.AspNet.Commands.Blazor.BlazorCrud;

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

    internal static CodeSnippet AddRazorComponentsSnippet = new()
    {
        Block = "WebApplication.CreateBuilder.Services.AddRazorComponents()",
        InsertBefore = new string[] { "var app = WebApplication.CreateBuilder.Build()" },
        CodeChangeType = CodeChangeType.Default,
        LeadingTrivia = new Formatting()
        {
            Newline = true,
            NumberOfSpaces = 0
        }
    };

    internal static CodeSnippet AddMapRazorComponentsSnippet = new()
    {
        Block = "app.MapRazorComponents<App>()",
        InsertBefore = [ "app.Run()" ],
        CodeChangeType = CodeChangeType.Default,
        LeadingTrivia = new Formatting()
        {
            Newline = true,
            NumberOfSpaces = 0
        }
    };

    internal static CodeSnippet AddInteractiveServerRenderModeSnippet = new()
    {
        Block = "AddInteractiveServerRenderMode()",
        Parent = "MapRazorComponents<App>",
        CodeChangeType = CodeChangeType.MemberAccess,
        LeadingTrivia = new Formatting()
        {
            Newline = true,
            NumberOfSpaces = 4
        }
    };

    internal static CodeSnippet AddInteractiveServerComponentsSnippet = new()
    {
        Block = "AddInteractiveServerComponents()",
        Parent = "WebApplication.CreateBuilder.Services.AddRazorComponents()",
        CodeChangeType = CodeChangeType.MemberAccess,
        LeadingTrivia = new Formatting()
        {
            Newline = true,
            NumberOfSpaces = 4
        }
    };

    internal static CodeSnippet AddInteractiveWebAssemblyRenderModeSnippet = new CodeSnippet()
    {
        Block = "AddInteractiveWebAssemblyRenderMode()",
        Parent = "MapRazorComponents<App>",
        CodeChangeType = CodeChangeType.MemberAccess,
        LeadingTrivia = new Formatting()
        {
            Newline = true,
            NumberOfSpaces = 4
        }
    };

    private static readonly Lazy<Dictionary<string, string>> _crudTemplates =
        new(() => new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            { "Create", CreateBlazorTemplate },
            { "Delete", DeleteBlazorTemplate },
            { "Details", DetailsBlazorTemplate },
            { "Edit", EditBlazorTemplate },
            { "Index", IndexBlazorTemplate }
        });

    internal static Dictionary<string, string> CRUDTemplates => _crudTemplates.Value;

    internal static ITextTransformation? GetBlazorCrudTransformation(string? templatePath)
    {
        if (string.IsNullOrEmpty(templatePath))
        {
            return null;
        }

        var host = new TextTemplatingEngineHost { TemplateFile = templatePath };
        ITextTransformation? transformation = null;

        switch (Path.GetFileName(templatePath))
        {
            case CreateBlazorTemplate:
                transformation = new Create() { Host = host };
                break;
            case IndexBlazorTemplate:
                transformation = new Templates.BlazorCrud.Index() { Host = host };
                break;
            case DeleteBlazorTemplate:
                transformation = new Delete() { Host = host };
                break;
            case EditBlazorTemplate:
                transformation = new Edit() { Host = host };
                break;
            case DetailsBlazorTemplate:
                transformation = new Details() { Host = host };
                break;
        }

        if (transformation is not null)
        {
            transformation.Session = host.CreateSession();
        }

        return transformation;
    }

    internal static IList<string> GetT4Templates(string templateName)
    {
        var templates = new List<string>();
        var crudTemplate = string.Equals(templateName, CrudPageType, StringComparison.OrdinalIgnoreCase);
        if (crudTemplate)
        {
            templates.AddRange([.. CRUDTemplates.Values]);
        }
        else if (CRUDTemplates.TryGetValue(templateName, out var t4Template))
        {
            templates.Add(t4Template);
        }
        else
        {
            //we should not have gotten here since we already validated this data.
            //default to "CRUD" template
            templates.AddRange([.. CRUDTemplates.Values]);
        }

        return templates;
    }

    internal static string ValidateAndGetOutputPath(string modelName, string templateName, string? projectPath)
    {
        string outputFileName = string.IsNullOrEmpty(modelName) ?
            $"{templateName}{Constants.BlazorExtension}" :
            Path.Combine($"{modelName}Pages", $"{templateName}{Constants.BlazorExtension}");
        string projectBasePath = Path.GetDirectoryName(projectPath) ?? Directory.GetCurrentDirectory();
        string outputFolder = Path.Combine(projectBasePath, "Components", "Pages");

        var outputPath = Path.Combine(outputFolder, outputFileName);
        return outputPath;
    }

    internal static async Task<BlazorCrudAppProperties> GetBlazorPropertiesAsync(Document? programDocument, Document? appRazorDocument)
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

    internal static CodeModifierConfig AddBlazorChangesToCodeFile(CodeModifierConfig configToEdit, BlazorCrudAppProperties appProperties)
    {
        var programCsFile = configToEdit.Files?.FirstOrDefault(x => !string.IsNullOrEmpty(x.FileName) && x.FileName.Equals("Program.cs", StringComparison.OrdinalIgnoreCase));
        var globalMethod = programCsFile?.Methods?["Global"];
        if (globalMethod is null)
        {
            return configToEdit;
        }

        var codeChanges = globalMethod.CodeChanges?.ToHashSet();
        if (appProperties.AddRazorComponentsExists)
        {
            if (!appProperties.InteractiveWebAssemblyComponentsExists && !appProperties.InteractiveServerComponentsExists)
            {
                codeChanges?.Add(AddInteractiveServerComponentsSnippet);
                codeChanges?.Add(AddInteractiveServerRenderModeSnippet);
            }
        }
        else
        {
            codeChanges?.Add(AddRazorComponentsSnippet);
            codeChanges?.Add(AddInteractiveServerComponentsSnippet);
            codeChanges?.Add(AddInteractiveServerRenderModeSnippet);
        }

        if (appProperties.MapRazorComponentsExists)
        {
            if (appProperties.InteractiveServerRenderModeNeeded)
            {
                codeChanges?.Add(AddInteractiveServerRenderModeSnippet);
            }

            if (appProperties.InteractiveWebAssemblyRenderModeNeeded)
            {
                codeChanges?.Add(AddInteractiveWebAssemblyRenderModeSnippet);
            }
        }
        else
        {
            codeChanges?.Add(AddMapRazorComponentsSnippet);
            codeChanges?.Add(AddInteractiveServerRenderModeSnippet);
        }

        globalMethod.CodeChanges = codeChanges?.ToArray();

        if (programCsFile is not null && programCsFile.Methods is not null)
        {
            programCsFile.Methods["Global"] = globalMethod;
        }

        return configToEdit;
    }
}
