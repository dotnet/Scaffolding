// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.VisualStudio.Web.CodeGeneration.EntityFrameworkCore;

namespace Microsoft.VisualStudio.Web.CodeGenerators.Mvc.View
{
    public class ViewGeneratorTemplateModel
    {
        public string ViewDataTypeName { get; set; }

        public string ViewDataTypeShortName { get; set; }

        public string ViewName { get; set; }

        public string LayoutPageFile { get; set; }

        public bool IsPartialView { get; set; }

        public bool IsLayoutPageSelected { get; set; }

        public bool ReferenceScriptLibraries { get; set; }

        public IModelMetadata ModelMetadata { get; set; }

        public string JQueryVersion { get; set; }
        public string NullableEnabled { get; set; }
    }
}
