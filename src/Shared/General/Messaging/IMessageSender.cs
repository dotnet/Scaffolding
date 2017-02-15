// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.VisualStudio.Web.CodeGeneration.Contracts.Messaging;

namespace Microsoft.VisualStudio.Web.CodeGeneration.Utils.Messaging
{
    public interface IMessageSender
    {
        bool Send(Message message);
    }
}
