// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.Framework.CodeGeneration.CommandLine;

namespace Microsoft.Framework.CodeGenerators.Mvc
{
    public class ControllerGeneratorModel : CommanCommandLineModel
    {
        [Option(Name = "useAsyncActions", ShortName = "async", Description = "Switch to indicate whether to generate async controller actions")]
        public bool UseAsync { get; set; }

        [Option(Name = "noViews", ShortName = "nv", Description = "Switch to indicate whether to generate CRUD views")]
        public bool NoViews { get; set; }

        [Option(Name = "controllerName", ShortName = "name", Description = "Name of the controller")]
        public string ControllerName { get; set; }

        [Option(Name = "readWriteController", ShortName = "readwrite", Description = "Switch to generate read write controller actions")]
        public bool ReadWriteController { get; set; }

        [Option(Name = "emptyController", ShortName = "empty", Description = "Switch to generate empty controller")]
        public bool EmptyController { get; set; }
    }
}