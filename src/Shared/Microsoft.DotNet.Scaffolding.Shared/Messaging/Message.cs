// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Microsoft.DotNet.Scaffolding.Shared.Messaging
{
    /// <summary>
    /// Contains information passed between Scaffolding server and client.
    /// </summary>
    public class Message
    {
        /// <summary>
        /// An identifier for the sender of the message.
        /// </summary>
        public string HostId { get; set; }

        /// <summary>
        /// See <see cref="MessageTypes"/> for valid message types.
        /// </summary>
        public string MessageType { get; set; }

        /// <summary>
        /// Payload in json format.
        /// </summary>
        public JToken Payload { get; set; }

        /// <summary>
        /// The protocol version to use for communication.
        /// </summary>
        public int ProtocolVersion { get; set; }

        /// <inheritdoc/>
        public override string ToString()
        {
            return "(" + HostId + ", " + MessageType + ", " + ProtocolVersion + ") -> " + (Payload == null ? "null" : Payload.ToString(Formatting.Indented));
        }
    }
}
