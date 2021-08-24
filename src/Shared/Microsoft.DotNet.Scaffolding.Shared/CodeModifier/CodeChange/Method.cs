namespace Microsoft.DotNet.Scaffolding.Shared.CodeModifier.CodeChange
{
    public class Method
    {
        public string[] Parameters { get; set; }
        public CodeSnippet[] CodeChanges { get; set; }
    }
}
