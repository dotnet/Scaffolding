// Copyright (c) .NET Foundation. All rights reserved.

namespace Microsoft.VisualStudio.Web.CodeGenerators.Mvc.Identity
{
    public class IdentityGeneratorTemplateModel2 : IdentityGeneratorTemplateModel
    {
        public string BootstrapVersion { get; set; }

        // identifies which Identity content to scaffold.
        public string ContentVersion { get; set; }

        public bool IsRazorPagesProject { get; set; }

        public bool IsBlazorProject { get; set; }
    }
}
