// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

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
        public bool UseSQLite { get; set; }
        public bool IsUsingExistingDbContext { get; set; }
        public bool IsGenerateCustomUser { get; set; }
        public bool IsGeneratingIndividualFiles { get; set; }
        public IdentityGeneratorFile[] FilesToGenerate { get; set; }
        public bool UseDefaultUI { get; set; }
        public bool GenerateLayout { get; set; }
        public string Layout { get; set; }
        public string LayoutPageNoExtension { get; set; }
        public string SupportFileLocation { get; set; }
        public bool HasExistingNonEmptyWwwRoot { get; set; }
    }
}