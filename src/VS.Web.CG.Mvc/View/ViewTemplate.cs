// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.
using System;
using System.Collections.Generic;

namespace Microsoft.VisualStudio.Web.CodeGenerators.Mvc.View
{
    public class ViewTemplate
    {
        public static readonly ViewTemplate EmptyViewTemplate = new ViewTemplate("Empty", false);
        public static readonly ViewTemplate CreateViewTemplate = new ViewTemplate("Create", true);
        public static readonly ViewTemplate EditViewTemplate = new ViewTemplate("Edit", true);
        public static readonly ViewTemplate DeleteViewTemplate = new ViewTemplate("Delete", true);
        public static readonly ViewTemplate DetailsViewTemplate = new ViewTemplate("Details", true);
        public static readonly ViewTemplate ListViewTemplate = new ViewTemplate("List", true);

        public static readonly Dictionary<string, ViewTemplate> ViewTemplateNames = new Dictionary<string, ViewTemplate>(StringComparer.OrdinalIgnoreCase)
        {
            { EmptyViewTemplate.Name, EmptyViewTemplate },
            { CreateViewTemplate.Name, CreateViewTemplate },
            { EditViewTemplate.Name, EditViewTemplate },
            { DeleteViewTemplate.Name, DeleteViewTemplate },
            { DetailsViewTemplate.Name, DetailsViewTemplate },
            { ListViewTemplate.Name, ListViewTemplate },
        };

        private ViewTemplate (string templateName, bool isModelRequired)
        {
            if (string.IsNullOrEmpty(templateName)){
                throw new ArgumentException(nameof(templateName));
            }

            Name = templateName;
            IsModelRequired = isModelRequired;
        }
        public string Name { get; }
        public bool IsModelRequired { get; }
    }
}