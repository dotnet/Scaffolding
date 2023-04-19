// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.DotNet.Scaffolding.Shared.Project;

namespace Microsoft.VisualStudio.Web.CodeGenerators.Mvc.Razor
{
    public class RazorPageWithContextTemplateModel2 : RazorPageWithContextTemplateModel
    {
        public RazorPageWithContextTemplateModel2(ModelType modelType, string dbContextFullTypeName)
            : base(modelType, dbContextFullTypeName)
        {
        }

        public string BootstrapVersion { get; set; }

        // identifies which Identity content to scaffold.
        public string ContentVersion { get; set; }
    }
}
