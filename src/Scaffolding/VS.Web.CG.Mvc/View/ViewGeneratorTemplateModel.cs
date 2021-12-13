// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
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
        public bool NullableEnabled { get; set; }
    }
}
