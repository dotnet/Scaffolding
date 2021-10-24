using System.Collections.Generic;

namespace Microsoft.DotNet.Scaffolding.Shared.CodeModifier.CodeChange
{
    public class CodeFile
    {
        public Dictionary<string, Method> Methods { get; set; }
        public string[] Usings { get; set; }
        public CodeBlock[] UsingsWithOptions { get; set; }
        public string FileName { get; set; }
        public CodeBlock[] ClassProperties { get; set; }
        public CodeBlock[] ClassAttributes { get; set; }
        public string[] GlobalVariables { get; set; }
        public string[] Options { get; set; }
    }

    public class CodeBlock 
    {
        public string Block { get ; set; }
        public string[] Options { get; set; }
    }

    public class Formatting
    {
        public bool Newline { get ; set; }
        public int NumberOfSpaces { get; set; }
    }
}
