// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.VisualStudio.Web.CodeGeneration.EntityFrameworkCore;

// TODO: determine exactly what is needed here. Initially, this is a copy of ViewGeneratorTemplateModel
namespace Microsoft.VisualStudio.Web.CodeGenerators.Mvc.Razor
{
    public class RazorPageGeneratorTemplateModel
    {

        public string NamespaceName { get; set; }

        public bool NoPageModel { get; set; }

        public string PageModelClassName { get; set; }

        public string RazorPageName { get; set; }

        public string LayoutPageFile { get; set; }

        public bool IsPartialView { get; set; }

        public bool IsLayoutPageSelected { get; set; }

        public bool ReferenceScriptLibraries { get; set; }

        public IModelMetadata ModelMetadata { get; set; }

        public string JQueryVersion { get; set; }
        public string NullableEnabled { get; set; }

    }
}
