// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.DotNet.Scaffolding.Shared.Messaging;

namespace Microsoft.DotNet.Scaffolding.Shared.Messaging
{
    public interface IMessageSender
    {
        bool Send(Message message);

        Message CreateMessage(MessageType messageType, object o, int protocolVersion);
    }
}
