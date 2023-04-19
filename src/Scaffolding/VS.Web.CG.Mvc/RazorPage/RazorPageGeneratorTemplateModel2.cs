// Copyright (c) .NET Foundation. All rights reserved.

namespace Microsoft.VisualStudio.Web.CodeGenerators.Mvc.Razor
{
    public class RazorPageGeneratorTemplateModel2 : RazorPageGeneratorTemplateModel
    {
        public string BootstrapVersion { get; set; }

        // identifies which Identity content to scaffold.
        public string ContentVersion { get; set; }
    }
}
