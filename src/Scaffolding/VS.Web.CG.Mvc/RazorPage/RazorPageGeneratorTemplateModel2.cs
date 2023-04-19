// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.VisualStudio.Web.CodeGenerators.Mvc.Razor
{
    public class RazorPageGeneratorTemplateModel2 : RazorPageGeneratorTemplateModel
    {
        public string BootstrapVersion { get; set; }

        // identifies which Identity content to scaffold.
        public string ContentVersion { get; set; }
    }
}
