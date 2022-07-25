using System;
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
            State = state;
            Content = content;
            Command = command ?? throw new ArgumentNullException(nameof(command));
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
