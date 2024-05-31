// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using System.Text.Json;

namespace Microsoft.DotNet.Scaffolding.Helpers.Services;

internal class JsonResponse
{
    public string Command { get; }
    public string? State { get; set; }
    public object? Content { get; set; }
    public string? Output { get; set; }

    public JsonResponse(string command, string? state = null, object? content = null, string? output = null)
    {
        Command = command;
        State = state;
        Content = content;
        Output = output;
    }

    public string ToJsonString()
    {
        return JsonSerializer.Serialize(this);
    }
}

public class State
{
    public const string Success = nameof(Success);
    public const string Processing = nameof(Processing);
    public const string Fail = nameof(Fail);
}
