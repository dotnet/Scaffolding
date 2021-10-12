using System;

namespace Microsoft.DotNet.Scaffolding.Shared.CodeModifier.CodeChange
{
    public class Method
    {
        public string[] Parameters { get; set; }
        public CodeBlock [] AddParameters { get; set; }
        public Tuple<string, string> EditParameters { get; set; }
        public CodeSnippet[] CodeChanges { get; set; }
    }
}
