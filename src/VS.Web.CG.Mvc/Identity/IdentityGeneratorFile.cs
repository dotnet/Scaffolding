// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.
using System.Collections.Generic;

namespace Microsoft.VisualStudio.Web.CodeGenerators.Mvc.Identity
{
    public class IdentityGeneratorFile
    {
        public string Name { get; set; }
        public string SourcePath { get; set; }
        public string OutputPath { get; set; }
        public bool IsTemplate { get; set; }
        public bool ShowInListFiles { get; set; } = true;
        public OverWriteCondition ShouldOverWrite { get; set; } = OverWriteCondition.WithForce;
    }

    internal class IdentityGeneratorFiles
    {
        public Dictionary<string, IdentityGeneratorFile[]> NamedFileConfig { get; set; }
    }
}