// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Newtonsoft.Json.Linq;
using Microsoft.VisualStudio.Web.CodeGeneration.Contracts.Messaging;
using Microsoft.VisualStudio.Web.CodeGeneration.Contracts.ProjectModel;

namespace Microsoft.VisualStudio.Web.CodeGeneration.Utils.Messaging
{
    public class ScaffoldingMessageHandler
    {
        private readonly ILogger _logger;
        private readonly string _hostId;

        public ScaffoldingMessageHandler(ILogger logger, string hostId)
        {
            if(logger == null)
            {
                throw new ArgumentNullException(nameof(logger));
            }

            _logger = logger;
            _hostId = hostId;
        }

        public IProjectContext ProjectInfo { get; set; }

        public void HandleMessages(object sender, Message e)
        {
            switch (e.MessageType)
            {
                case MessageTypes.ProjectInfoRequest:
                    SendProjectInfo(sender as IMessageSender);
                    break;
                case MessageTypes.ProjectInfoResponse:
                    BuildDependencyProviderFromResponse(e);
                    break;
                default:
                    _logger.LogMessage($"Unknown message type {e.MessageType}");
                    break;
            }
        }

        private void BuildDependencyProviderFromResponse(Message msg)
        {
            try
            {
                ProjectInfo = msg.Payload.ToObject<CommonProjectContext>();
            }
            catch (Exception ex)
            {
                _logger.LogMessage($"Failed to build Dependency information from message.{Environment.NewLine}{msg?.ToString()}");
                throw ex;
            }
        }

        private void SendProjectInfo(IMessageSender sender)
        {
            if (sender == null)
            {
                throw new ArgumentNullException(nameof(sender));
            }

            try
            {
                var message = new Message()
                {
                    MessageType = MessageTypes.ProjectInfoResponse,
                    HostId = _hostId,
                    Payload = JToken.FromObject(ProjectInfo)
                };

                sender.Send(message);
            }
            catch (Exception ex)
            {
                _logger.LogMessage($"Failed to send dependency information message. {Environment.NewLine}{ex.Message}");
            }
        }
    }
}
