// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using System.Text.Json.Serialization;

namespace Microsoft.DotNet.Scaffolding.Helpers.Roslyn;

[JsonConverter(typeof(JsonStringEnumConverter))]
internal enum CodeChangeType
{
    Default,
    MemberAccess,
    Lambda
}

internal class CodeChangeOptionStrings
{
    public const string IfStatement = nameof(IfStatement);
    public const string ElseStatement = nameof(ElseStatement);
    public const string MicrosoftGraph = nameof(MicrosoftGraph);
    public const string DownstreamApi = nameof(DownstreamApi);
    public const string Skip = nameof(Skip);
    public const string NonMinimalApp = nameof(NonMinimalApp);
    public const string MinimalApp = nameof(MinimalApp);
    public const string OpenApi = nameof(OpenApi);
    public const string EfScenario = nameof(EfScenario);
}

internal class CodeChangeOptions
{
    public bool MicrosoftGraph { get; set; } = false;
    public bool DownstreamApi { get; set; } = false;
    public bool IsMinimalApp { get; set; } = false;
    public bool UsingTopLevelsStatements { get; set; } = true;
    public bool EfScenario { get; set; }
}
