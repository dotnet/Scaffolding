// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Microsoft.DotNet.Scaffolding.Shared;
using Microsoft.DotNet.Scaffolding.Shared.Project;
using Microsoft.VisualStudio.Web.CodeGeneration.CommandLine;

namespace Microsoft.VisualStudio.Web.CodeGenerators.Mvc.Blazor
{
    public class BlazorWebCRUDGeneratorCommandLineModel
    {

        [Option(Name = "model", ShortName = "m", Description = "Model class to use")]
        public string ModelClass { get; set; }

        [Argument(Description = "The view template to use. Supported view templates: 'Empty|Create|Edit|Delete|Details|Index|CRUD'")]
        public string TemplateName { get; set; } = "crud";

        [Option(Name = "dataContext", ShortName = "dc", Description = "DbContext class to use")]
        public string DataContextClass { get; set; }

        [Option(Name = "relativeFolderPath", ShortName = "outDir", Description = "Specify the relative output folder path from project where the file needs to be generated, if not specified, file will be generated in the project folder")]
        public string RelativeFolderPath { get; set; }

        [Option(Name = "namespaceName", ShortName = "namespace", Description = "Specify the name of the namespace to use for the generated blazor pages")]
        public string Namespace { get; set; }

        [Option(Name = "databaseProvider", ShortName = "dbProvider", Description = "Database provider to use. Options include 'sqlserver' (default), 'sqlite', 'cosmos', 'postgres'.")]
        public string DatabaseProviderString { get; set; }
        public DbProvider DatabaseProvider { get; set; }

        public BlazorWebCRUDGeneratorCommandLineModel()
        {
        }

        protected BlazorWebCRUDGeneratorCommandLineModel(BlazorWebCRUDGeneratorCommandLineModel copyFrom)
        {
            ModelClass = copyFrom.ModelClass;
            TemplateName = copyFrom.TemplateName;
            DataContextClass = copyFrom.DataContextClass;
            RelativeFolderPath = copyFrom.RelativeFolderPath;
            Namespace = copyFrom.Namespace;
            DatabaseProviderString = copyFrom.DatabaseProviderString;
            DatabaseProvider = copyFrom.DatabaseProvider;
        }

        public BlazorWebCRUDGeneratorCommandLineModel Clone()
        {
            return new BlazorWebCRUDGeneratorCommandLineModel(this);
        }
    }

    public static class BlazorWebCRUDGeneratorCommandLineModelExtensions
    {
        public static void ValidateCommandline(this BlazorWebCRUDGeneratorCommandLineModel model)
        {
            ArgumentNullException.ThrowIfNull(model.ModelClass);
            ArgumentNullException.ThrowIfNull(model.TemplateName);

            List<string> errorList = new List<string>();
            List<string> templateNames = new List<string>()
            {
                "empty", "create", "edit", "delete", "details", "index", "crud"
            };

            if (!string.IsNullOrEmpty(model.Namespace) &&
                !RoslynUtilities.IsValidNamespace(model.Namespace))
            {
                errorList.Add(string.Format(
                    CultureInfo.CurrentCulture,
                    MessageStrings.InvalidNamespaceName,
                    model.Namespace));
            }

            if (errorList.Count != 0)
            {
                throw new InvalidOperationException(string.Join("\n", errorList.ToArray()));
            }

            if (!string.IsNullOrEmpty(model.DatabaseProviderString) && EfConstants.AllDbProviders.TryGetValue(model.DatabaseProviderString, out var dbProvider))
            {
                model.DatabaseProvider = dbProvider;
            }

            if (string.IsNullOrEmpty(model.TemplateName))
            {
                model.TemplateName = "crud";
            }
            else if (!templateNames.Contains(model.TemplateName, StringComparer.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException("Invalid template name specified. Supported templates are: " + string.Join(", ", templateNames));
            }
        }
    }
}
