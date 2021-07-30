// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

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
            return "(" + HostId + ", " + MessageType + ", " + ProtocolVersion+") -> " + (Payload == null ? "null" : Payload.ToString(Formatting.Indented));
        }
    }
}
