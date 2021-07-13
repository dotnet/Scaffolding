using System.Collections.Generic;

namespace Microsoft.DotNet.MSIdentity.CodeReaderWriter.CodeChange
{
    public class CodeFile
    {
        public Dictionary<string, Method>? Methods { get; set; }
        public string[]? Usings { get; set; }
        public string? FileName { get; set; }
        public string[]? ClassProperties { get; set; }
        public string[]? ClassAttributes { get; set; }
    }
}
