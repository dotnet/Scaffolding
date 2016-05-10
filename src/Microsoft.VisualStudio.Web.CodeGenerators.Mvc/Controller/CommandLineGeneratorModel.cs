// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.VisualStudio.Web.CodeGeneration.CommandLine;

namespace Microsoft.VisualStudio.Web.CodeGenerators.Mvc.Controller
{
    public class CommandLineGeneratorModel : CommonCommandLineModel
    {
        [Option(Name = "useAsyncActions", ShortName = "async", Description = "Switch to indicate whether to generate async controller actions")]
        public bool UseAsync { get; set; }

        [Option(Name = "noViews", ShortName = "nv", Description = "Switch to indicate whether to generate CRUD views")]
        public bool NoViews { get; set; }

        [Option(Name = "controllerName", ShortName = "name", Description = "Name of the controller")]
        public string ControllerName { get; set; }

        [Option(Name = "restWithNoViews", ShortName = "api", Description = "Specify this switch to generate a Controller with REST style API, noViews is assumed and any view related options are ignored")]
        public bool IsRestController { get; set; }

        [Option(Name = "readWriteActions", ShortName = "actions", Description = "Specify this switch to generate Controller with read/write actions when a Model class is not used")]
        public bool GenerateReadWriteActions { get; set; }
    }
}