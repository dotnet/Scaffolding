namespace Microsoft.DotNet.Scaffolding.Shared.CodeModifier.CodeChange
{
    public class CodeSnippet
    {
        public string InsertAfter { get; set; }
        public string Block { get; set; }
        public string CheckBlock { get; set; }
        public string Parent { get; set; }
        public bool Append { get; set; } = false;
        public string Parameter { get; set; }
        public string[] InsertBefore { get; set; }
        public string[] Options { get; set; }
        public Formatting CodeFormatting { get; set; }
    }
}
