using Microsoft.VisualStudio.Web.CodeGeneration;
using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.VisualStudio.Web.CodeGenerators.Mvc.Razor
{
    public class RazorPageWithContextTemplateModel2 : RazorPageWithContextTemplateModel
    {
        public RazorPageWithContextTemplateModel2(ModelType modelType, string dbContextFullTypeName)
            :base(modelType, dbContextFullTypeName)
        {
        }

        public string BootstrapVersion { get; set; }

        // identifies which Identity content to scaffold.
        public string ContentVersion { get; set; }
    }
}
