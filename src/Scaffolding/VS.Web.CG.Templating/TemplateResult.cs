// Copyright (c) .NET Foundation. All rights reserved.

using System;
using System.Collections.Generic;

namespace Microsoft.VisualStudio.Web.CodeGeneration.Templating
{
    public class TemplateResult
    {
        public string GeneratedText { get; set; }

        public TemplateProcessingException ProcessingException { get; set; }
    }
}
