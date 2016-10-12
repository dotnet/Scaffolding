// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Microsoft.VisualStudio.Web.CodeGeneration.Utils.Messaging
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
