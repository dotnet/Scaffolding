// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.DotNet.Scaffolding.Shared;

namespace Microsoft.VisualStudio.Web.CodeGenerators.Mvc.Identity
{
    public class IdentityGeneratorTemplateModel
    {
        public string Namespace { get; set; }
        public string ApplicationName { get; set; }
        public string UserClass { get; set; }
        public string UserClassNamespace { get; set; }
        public string DbContextClass { get; set; }
        public string DbContextNamespace { get; set; }
        public DbProvider DatabaseProvider { get; set; }
        public bool IsUsingExistingDbContext { get; set; }
        public bool IsGenerateCustomUser { get; set; }
        public IdentityGeneratorFile[] FilesToGenerate { get; set; }
        public bool UseDefaultUI { get; set; }
        public bool GenerateLayout { get; set; }
        public string Layout { get; set; }
        public string LayoutPageNoExtension { get; set; }
        public string SupportFileLocation { get; set; }
        public bool HasExistingNonEmptyWwwRoot { get; set; }
    }
}
