// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;

namespace Microsoft.Framework.CodeGeneration.Templating
{
    public class TemplateResult
    {
        public string GeneratedText { get; set; }

        public TemplateProcessingException ProcessingException { get; set; }
    }
}