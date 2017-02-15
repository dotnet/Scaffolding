using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Microsoft.VisualStudio.Web.CodeGeneration.Contracts.Messaging
{
    public class Message
    {
        public string HostId { get; set; }
        public string MessageType { get; set; }
        public JToken Payload { get; set; }

        public override string ToString()
        {
            return "(" + HostId + ", " + MessageType + ") -> " + (Payload == null ? "null" : Payload.ToString(Formatting.Indented));
        }
    }
}
