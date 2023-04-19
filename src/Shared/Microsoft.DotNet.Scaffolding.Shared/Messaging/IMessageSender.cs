// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/* Unmerged change from project 'Microsoft.DotNet.Scaffolding.Shared (net8.0)'
Before:
// Copyright (c) .NET Foundation. All rights reserved.
After:
// Copyright (c) .NET Foundation under one or more agreements.
*/
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.DotNet.Scaffolding.Shared.Messaging
{
    public interface IMessageSender
    {
        bool Send(Message message);

        Message CreateMessage(MessageType messageType, object o, int protocolVersion);
    }
}
