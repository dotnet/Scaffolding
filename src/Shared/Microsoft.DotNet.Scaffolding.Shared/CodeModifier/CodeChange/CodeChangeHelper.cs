namespace Microsoft.DotNet.Scaffolding.Shared.CodeModifier.CodeChange
{
    public class CodeChangeType
    {
        public const string MemberAccess = nameof(MemberAccess);
        public const string InLambdaBlock = nameof(InLambdaBlock);
        public const string LambdaExpression = nameof(LambdaExpression);
    }

    public class CodeChangeOptionStrings
    {
        public const string IfStatement = nameof(IfStatement);
        public const string ElseStatement = nameof(ElseStatement);
        public const string MicrosoftGraph = nameof(MicrosoftGraph);
        public const string DownstreamApi = nameof(DownstreamApi);
        public const string Skip = nameof(Skip);
        public const string NonMinimalApp = nameof(NonMinimalApp);
    }

    public class CodeChangeOptions
    {
        public bool MicrosoftGraph { get; set; } = false;
        public bool DownstreamApi { get; set; } = false;
        public bool IsMinimalApp { get; set; } = false;
    }
}
