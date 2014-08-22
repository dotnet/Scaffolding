// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.Framework.CodeGeneration.CommandLine;

namespace Microsoft.Framework.CodeGenerators.Mvc
{
    public class DependencyGeneratorModel
    {
        [Option(Name = "addStaticFiles", ShortName = "sf", Description = "Adds static files support to the project")]
        public bool AddStaticFiles { get; set; }

        [Option(Name = "addMvcLayout", ShortName = "ml", Description = "Adds MVC Layout to the project")]
        public bool AddMvcLayout { get; set; }
    }
}