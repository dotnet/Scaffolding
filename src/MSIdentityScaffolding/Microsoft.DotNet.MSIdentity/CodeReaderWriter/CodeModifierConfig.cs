using Microsoft.DotNet.MSIdentity.CodeReaderWriter.CodeChange;

namespace Microsoft.DotNet.MSIdentity.CodeReaderWriter
{
    public class CodeModifierConfig
    {
        public string? Identifier { get; set; }
        public CodeFile[]? Files { get; set; }
    }
}
