namespace Microsoft.DotNet.Scaffolding.Shared.CodeModifier.CodeChange
{
    public class CodeSnippet
    {
        public string InsertAfter { get; set; }
        public string Block { get; set; }
        public string Parent { get; set; }
        public string Type { get; set; }
        public bool Append { get; set; } = false;
        public string Parameter { get; set; }
        public bool IfStatement { get; set; }
        public bool ElseStatement { get; set; }
        public string[] InsertBefore { get; set; }
    }
}
