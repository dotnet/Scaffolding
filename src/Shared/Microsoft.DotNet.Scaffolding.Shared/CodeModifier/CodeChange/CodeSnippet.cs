namespace Microsoft.DotNet.Scaffolding.Shared.CodeModifier.CodeChange
{
    public class CodeSnippet
    {
        public string InsertAfter { get; set; }
        public string Block { get; set; }
        public string CheckBlock { get; set; }
        public string Parent { get; set; }
        public bool Prepend { get; set; } = false;
        public string[] InsertBefore { get; set; }
        public string[] Options { get; set; }
        public Formatting CodeFormatting { get; set; }
        public string ReplaceSnippet { get; set; }
        public string Parameter { get; set; }
        public CodeChangeType CodeChangeType { get; set; } = CodeChangeType.Default;
    }
}
