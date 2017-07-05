// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.VisualStudio.Web.CodeGeneration.CommandLine;

namespace Microsoft.VisualStudio.Web.CodeGenerators.Mvc.Razor
{
    public class RazorPageGeneratorModel : CommonCommandLineModel
    {
        public string ViewName { get; set; }

        [Argument(Description = "The view template to use, supported view templates: 'Empty|Create|Edit|Delete|Details|List'")]
        public string TemplateName { get; set; }

        [Option(Name = "partialView", ShortName = "partial", Description = "Generate a partial view, other layout options (-l and -udl) are ignored if this is specified")]
        public bool PartialView { get; set; }

        [Option(Name ="noPageModel", ShortName ="npm", Description =" Switch to not generate a PageModel class for Empty template.")]
        public bool NoPageModel { get; set; }

        [Option(Name ="namespaceName", ShortName ="", Description = "")]
        public string NamespaceName { get; set; }

        public RazorPageGeneratorModel()
        {
        }

        protected RazorPageGeneratorModel(RazorPageGeneratorModel copyFrom)
            : base(copyFrom)
        {
            ViewName = copyFrom.ViewName;
            TemplateName = copyFrom.TemplateName;
            PartialView = copyFrom.PartialView;
        }

        public override CommonCommandLineModel Clone()
        {
            return new RazorPageGeneratorModel(this);
        }
    }
}
