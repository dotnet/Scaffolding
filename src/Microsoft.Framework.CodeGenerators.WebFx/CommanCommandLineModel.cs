// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.Framework.CodeGeneration.CommandLine;

namespace Microsoft.Framework.CodeGenerators.WebFx
{
    //Command line parameters common to controller and view scaffolder.
    public abstract class CommanCommandLineModel
    {
        [Option(Name = "model", ShortName = "m", Description = "Model class to use")]
        public string ModelClass { get; set; }

        [Option(Name = "dataContext", ShortName = "dc", Description = "DbContext class to use")]
        public string DataContextClass { get; set; }

        [Option(Name = "referenceScriptLibraries", ShortName = "scripts")]
        public bool ReferenceScriptLibraries { get; set; }

        [Option(Name = "layout", ShortName = "l", Description = "Layout page to use, pass empty string if set in a Razor _viewStart file")]
        public string LayoutPage { get; set; }

        [Option(Name = "useLayout", ShortName = "ul", Description = "Switch to specify whether to use a layout or not, if this is not present, --layout is ignored")]
        public bool UseLayout { get; set; }

        [Option(Name = "force", ShortName = "f", Description = "Use this option to overwrite existing files")]
        public bool Force { get; set; }
    }
}