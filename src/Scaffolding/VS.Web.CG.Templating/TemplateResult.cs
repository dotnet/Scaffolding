// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

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