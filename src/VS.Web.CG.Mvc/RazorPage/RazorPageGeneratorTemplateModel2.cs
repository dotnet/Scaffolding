// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.VisualStudio.Web.CodeGenerators.Mvc.Razor
{
    public class RazorPageGeneratorTemplateModel2 : RazorPageGeneratorTemplateModel
    {
        public string BootstrapVersion { get; set; }

        // identifies which Identity content to scaffold.
        public string ContentVersion { get; set; }
    }
}
