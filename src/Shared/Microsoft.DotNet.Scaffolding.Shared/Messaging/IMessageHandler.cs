// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.DotNet.Scaffolding.Shared.Messaging;

namespace Microsoft.DotNet.Scaffolding.Shared.Messaging
{
    public interface IMessageHandler
    {
        bool HandleMessage(IMessageSender sender, Message message);
    }
}