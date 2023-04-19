// Copyright (c) .NET Foundation. All rights reserved.
using Microsoft.DotNet.Scaffolding.Shared;
using Microsoft.DotNet.Scaffolding.Shared.Project;
using Microsoft.VisualStudio.Web.CodeGeneration.EntityFrameworkCore;

namespace Microsoft.VisualStudio.Web.CodeGenerators.Mvc
{
    public class ModelTypeAndContextModel
    {
        public ModelType ModelType { get; set; }

        public ContextProcessingResult ContextProcessingResult { get; set; }

        public string DbContextFullName { get; set; }

        public DbProvider DatabaseProvider { get; set; }
    }
}
