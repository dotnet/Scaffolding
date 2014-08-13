// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.Framework.CodeGeneration.CommandLine;

namespace Microsoft.Framework.CodeGenerators.Mvc
{
    public class ViewGeneratorModel : CommanCommandLineModel
    {
        public string ViewName { get; set; }

        [Argument(Description = "The view template to use, supported view templates: 'Create|Edit|Delete|Details|List'")]
        public string TemplateName { get; set; }

        [Option(Name = "partialView", ShortName = "partial")]
        public bool PartialView { get; set; }
    }
}