// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json;

namespace Microsoft.DotNet.MSIdentity.Shared
{
    public class JsonResponse
    {
        public string Command { get; }
        public string State { get; set; }
        public object Content { get; set; }
        public string Output { get; set; }

        public JsonResponse(string command, string state = null, object content = null, string output = null)
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

    internal class State
    {
        internal const string Success = nameof(Success);
        internal const string Processing = nameof(Processing);
        internal const string Fail = nameof(Fail);
    }
}
