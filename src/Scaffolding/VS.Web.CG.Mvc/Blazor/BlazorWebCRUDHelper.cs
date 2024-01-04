// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.DotNet.Scaffolding.Shared;
using Microsoft.DotNet.Scaffolding.Shared.CodeModifier.CodeChange;
using Microsoft.DotNet.Scaffolding.Shared.T4Templating;
using Microsoft.VisualStudio.Web.CodeGenerators.Mvc.Templates.Blazor;

namespace Microsoft.VisualStudio.Web.CodeGenerators.Mvc.Blazor
{
    internal static class BlazorWebCRUDHelper
    {
        internal const string Main = nameof(Main);

        //Template info
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

        internal static CodeSnippet AddRazorComponentsSnippet = new CodeSnippet()
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

        internal static CodeSnippet AddMapRazorComponentsSnippet = new CodeSnippet()
        {
            Block = "app.MapRazorComponents<App>()",
            InsertBefore = new string[] { "app.Run()" },
            CodeChangeType = CodeChangeType.Default,
            LeadingTrivia = new Formatting()
            {
                Newline = true,
                NumberOfSpaces = 0
            }
        };

        internal static CodeSnippet AddInteractiveServerRenderModeSnippet = new CodeSnippet()
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

        internal static CodeSnippet AddInteractiveServerComponentsSnippet = new CodeSnippet()
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

        internal static Dictionary<string, string> _crudTemplates;
        internal static Dictionary<string, string> CRUDTemplates
        {
            get
            {
                if (_crudTemplates == null)
                {
                    _crudTemplates = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                    {
                        { "Create", CreateBlazorTemplate },
                        { "Delete", DeleteBlazorTemplate },
                        { "Details", DetailsBlazorTemplate },
                        { "Edit",  EditBlazorTemplate },
                        { "Index", IndexBlazorTemplate }
                    };
                }

                return _crudTemplates;
            }
        }

        internal static IList<string> GetT4Templates(string templateName, ILogger logger)
        {
            var templates = new List<string>();
            var crudTemplate = string.Equals(templateName, "crud", StringComparison.OrdinalIgnoreCase);
            if (crudTemplate)
            {
                return CRUDTemplates.Values.ToList();
            }
            else if (CRUDTemplates.TryGetValue(templateName, out var t4Template))
            {
                templates.Add(t4Template);
            }
            else
            {
                logger.LogMessage($"Invalid template for the Blazor CRUD scaffolder '{templateName}' entered!", LogMessageLevel.Error);
                throw new ArgumentException(templateName);
            }

            return templates;
        }

        internal static ITextTransformation GetBlazorTransformation(string templatePath)
        {
            if (string.IsNullOrEmpty(templatePath))
            {
                return null;
            }

            var host = new TextTemplatingEngineHost { TemplateFile = templatePath };
            ITextTransformation transformation = null;

            switch (Path.GetFileName(templatePath))
            {
                case "Create.tt":
                    transformation = new Create() { Host = host };
                    break;
                case "Index.tt":
                    transformation = new Templates.Blazor.Index() { Host = host };
                    break;
                case "Delete.tt":
                    transformation = new Delete() { Host = host };
                    break;
                case "Edit.tt":
                    transformation = new Edit() { Host = host };
                    break;
                case "Details.tt":
                    transformation = new Details() { Host = host };
                    break;
            }

            if (transformation != null)
            {
                transformation.Session = host.CreateSession();
            }

            return transformation;
        }
    }
}
