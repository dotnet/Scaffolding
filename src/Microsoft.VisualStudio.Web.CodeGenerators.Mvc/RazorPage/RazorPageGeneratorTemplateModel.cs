// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Microsoft.VisualStudio.Web.CodeGeneration;
using Microsoft.VisualStudio.Web.CodeGeneration.EntityFrameworkCore;

// TODO: determine exactly what is needed here. Initially, this is a copy of ViewGeneratorTemplateModel
namespace Microsoft.VisualStudio.Web.CodeGenerators.Mvc.Razor
{
    public class RazorPageGeneratorTemplateModel
    {
        public string NamespaceName { get; set; }

        public bool NoPageModel { get; set; }

        public string PageModelClassName { get; set; }

        public string ViewName { get; set; }

        public string LayoutPageFile { get; set; }

        public bool IsPartialView { get; set; }

        public bool IsLayoutPageSelected { get; set; }

        public bool ReferenceScriptLibraries { get; set; }

        public IModelMetadata ModelMetadata { get; set; }

        public string JQueryVersion { get; set; }
    }
}
