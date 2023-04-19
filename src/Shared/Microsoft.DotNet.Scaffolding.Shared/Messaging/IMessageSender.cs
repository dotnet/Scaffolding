// Copyright (c) .NET Foundation. All rights reserved.

using Microsoft.DotNet.Scaffolding.Shared.Messaging;

namespace Microsoft.DotNet.Scaffolding.Shared.Messaging
{
    public interface IMessageSender
    {
        bool Send(Message message);

        Message CreateMessage(MessageType messageType, object o, int protocolVersion);
    }
}
