// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System;
using System.IO;
using Microsoft.DotNet.Scaffolding.Helpers.T4Templating;
using Microsoft.DotNet.Tools.Scaffold.AspNet.Templates.BlazorCrud;
using Microsoft.DotNet.Tools.Scaffold.AspNet.Helpers;

namespace Microsoft.DotNet.Tools.Scaffold.AspNet.Commands.BlazorCrud;

internal static class BlazorCrudHelper
{
    internal static string CrudPageType = "CRUD";
    internal static List<string> CRUDPages = [CrudPageType, "Create", "Delete", "Details", "Edit", "Index"];
    internal const string CreateBlazorTemplate = "Create.tt";
    internal const string DeleteBlazorTemplate = "Delete.tt";
    internal const string DetailsBlazorTemplate = "Details.tt";
    internal const string EditBlazorTemplate = "Edit.tt";
    internal const string IndexBlazorTemplate = "Index.tt";

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
            templates.AddRange([..CRUDTemplates.Values]);
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
}
