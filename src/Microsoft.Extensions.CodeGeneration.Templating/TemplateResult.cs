// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;

namespace Microsoft.Extensions.CodeGeneration.Templating
{
    public class TemplateResult
    {
        public string GeneratedText { get; set; }

        public TemplateProcessingException ProcessingException { get; set; }
    }
}