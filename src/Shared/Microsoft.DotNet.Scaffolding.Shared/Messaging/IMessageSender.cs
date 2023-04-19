// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.DotNet.Scaffolding.Shared.Messaging;

namespace Microsoft.DotNet.Scaffolding.Shared.Messaging
{
    public interface IMessageSender
    {
        bool Send(Message message);

        Message CreateMessage(MessageType messageType, object o, int protocolVersion);
    }
}
