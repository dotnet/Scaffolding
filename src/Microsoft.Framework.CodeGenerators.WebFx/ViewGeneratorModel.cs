// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.Framework.CodeGeneration.CommandLine;

namespace Microsoft.Framework.CodeGenerators.WebFx
{
    public class ViewGeneratorModel
    {
        [Option(Name = "model", ShortName = "m", Description = "Model class to use")]
        public string ModelClass { get; set; }

        [Option(Name = "dataContext", ShortName = "dc", Description = "DbContext class to use")]
        public string DataContextClass { get; set; }

        [Option(Name = "partialView", ShortName = "partial")]
        public bool PartialView { get; set; }

        [Option(Name = "referenceScriptLibraries", ShortName = "scripts")]
        public bool ReferenceScriptLibraries { get; set; }

        [Option(Name = "layout", ShortName = "l", Description = "Layout page to use, pass empty string if set in a Razor _viewStart file")]
        public string LayoutPage { get; set; }
    }
}