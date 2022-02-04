using System.Text.Json.Serialization;

namespace Microsoft.DotNet.Scaffolding.Shared.CodeModifier.CodeChange
{
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum CodeChangeType
    {
        Default,
        MemberAccess,
        InLambdaBlock,
        LambdaExpression,
        LambdaParameter
    }

    public class CodeChangeOptionStrings
    {
        public const string IfStatement = nameof(IfStatement);
        public const string ElseStatement = nameof(ElseStatement);
        public const string MicrosoftGraph = nameof(MicrosoftGraph);
        public const string DownstreamApi = nameof(DownstreamApi);
        public const string Skip = nameof(Skip);
        public const string NonMinimalApp = nameof(NonMinimalApp);
        public const string MinimalApp = nameof(MinimalApp);
    }

    public class CodeChangeOptions
    {
        public bool MicrosoftGraph { get; set; } = false;
        public bool DownstreamApi { get; set; } = false;
        public bool IsMinimalApp { get; set; } = false;
    }
}
