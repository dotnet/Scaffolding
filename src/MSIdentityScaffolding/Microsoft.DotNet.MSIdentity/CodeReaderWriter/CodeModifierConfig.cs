using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.DotNet.MSIdentity.CodeReaderWriter
{
    public class CodeChange
    {
        public string? InsertAfter { get; set; }
        public string? Block { get; set; }
        public string? BlockB2C { get; set; }
        public string? Parent { get; set; }
        public string? Type { get; set; }
        public bool? Append { get; set; } = false;
    }

    public class Method
    {
        public string[]? Parameters { get; set; }
        public CodeChange[]? CodeChanges { get; set; }
    }

    public class CodeModifierConfig
    {
        public string? Identifier { get; set; }
        public CodeFile[]? Files { get; set; }
       
    }

    public class CodeFile
    {
        public Dictionary<string, Method>? Methods { get; set; }
        public string[]? Usings { get; set; }
        public string? FileName { get; set; }
        public string[]? ClassProperties { get; set; }
        public string[]? ClassAttributes { get; set; }
    }

    public class CodeChangeType
    {
        public const string MemberAccess = nameof(MemberAccess);
        public const string InLambdaBlock = nameof(InLambdaBlock);
    }
}
