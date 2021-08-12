using Microsoft.DotNet.Scaffolding.Shared.CodeModifier.CodeChange;

namespace Microsoft.DotNet.Scaffolding.Shared.CodeModifier
{
    public class CodeModifierConfig
    {
        public string Identifier { get; set; }
        public CodeFile[] Files { get; set; }
    }
}
