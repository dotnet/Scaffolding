// Copyright (c) .NET Foundation. All rights reserved.
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
        public List<string> AltPaths { get; set; } = new List<string>();
    }

    internal class IdentityGeneratorFiles
    {
        public Dictionary<string, IdentityGeneratorFile[]> NamedFileConfig { get; set; }
    }
}
