namespace Microsoft.VisualStudio.Web.CodeGeneration.Tools
{
    public class ParamDefinition
    {
        public string Alias { get; set; }
        public string Description { get; set; }
        public Parameter[] Arguments { get;set; }
        public OptionParameter[] Options { get; set; }
    }

    public class Parameter
    {
        public string Name { get; set; }
        public string Description { get; set; }
    }

    public class OptionParameter : Parameter
    {
        public string ShortName { get; set; }
    }
}