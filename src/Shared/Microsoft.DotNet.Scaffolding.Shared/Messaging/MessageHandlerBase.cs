// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using Microsoft.DotNet.Scaffolding.Shared;
using Microsoft.DotNet.Scaffolding.Shared.Messaging;

namespace Microsoft.DotNet.Scaffolding.Shared.Messaging
{
    public abstract class MessageHandlerBase : IMessageHandler
    {

        public MessageHandlerBase(ILogger logger)
        {
            if (logger == null)
            {
                throw new ArgumentNullException(nameof(logger));
            }

            Logger = logger;
        }

        // Defines the current protocol version handled by the messageHandlers.
        public virtual int CurrentProtocolVersion => 1;
        protected ILogger Logger { get; }
        public abstract ISet<string> MessageTypesHandled { get; }

        public bool HandleMessage(IMessageSender sender, Message message)
        {
            if (sender == null)
            {
                throw new ArgumentNullException(nameof(sender));
            }

            if (message == null)
            {
                throw new ArgumentNullException(nameof(message));
            }

            if (!CanHandle(message))
            {
                return false;
            }

            if (message.ProtocolVersion != CurrentProtocolVersion)
            {
                Logger.LogMessage(
                    string.Format(MessageStrings.ProtocolVersionMismatch,
                        message.ProtocolVersion,
                        CurrentProtocolVersion),
                    LogMessageLevel.Warning);
            }

            return HandleMessageInternal(sender, message);
        }

        protected virtual bool CanHandle(Message message)
        {
            return MessageTypesHandled.Contains(message.MessageType);
        }

        protected abstract bool HandleMessageInternal(IMessageSender sender, Message message);
    }
}
