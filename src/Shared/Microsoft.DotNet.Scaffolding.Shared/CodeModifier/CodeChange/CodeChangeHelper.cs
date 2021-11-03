using System.Text.Json.Serialization;

namespace Microsoft.DotNet.Scaffolding.Shared.CodeModifier.CodeChange
{
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum CodeChangeType
    {
        Default,
        MemberAccess,
        Lambda
    }

    public class CodeChangeOptionStrings
    {
        public const string IfStatement = nameof(IfStatement);
        public const string ElseStatement = nameof(ElseStatement);
        public const string MicrosoftGraph = nameof(MicrosoftGraph);
        public const string DownstreamApi = nameof(DownstreamApi);
        public const string Skip = nameof(Skip);
        public const string NonMinimalApp = nameof(NonMinimalApp);
<<<<<<< HEAD
        public const string MinimalApp = nameof(MinimalApp);
        public const string OpenApi = nameof(OpenApi);
=======
>>>>>>> c9c71cf5 (Cherry picking main into release/6.0 (#1688))
    }

    public class CodeChangeOptions
    {
        public bool MicrosoftGraph { get; set; } = false;
        public bool DownstreamApi { get; set; } = false;
        public bool IsMinimalApp { get; set; } = false;
    }
}
