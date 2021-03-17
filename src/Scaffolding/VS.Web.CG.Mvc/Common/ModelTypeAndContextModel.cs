// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.VisualStudio.Web.CodeGeneration;
using Microsoft.VisualStudio.Web.CodeGeneration.EntityFrameworkCore;

namespace Microsoft.VisualStudio.Web.CodeGenerators.Mvc
{
    public class ModelTypeAndContextModel
    {
        public ModelType ModelType { get; set; }

        public ContextProcessingResult ContextProcessingResult { get; set; }

        public string DbContextFullName { get; set; }

        public bool UseSqlite { get; set; }
    }
}
