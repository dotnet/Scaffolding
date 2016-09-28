using System;
using Microsoft.VisualStudio.Web.CodeGeneration.ProjectInfo;
using Microsoft.VisualStudio.Web.CodeGeneration.Utils.Messaging;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;

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

        public ProjectInfoContainer ProjectInfo { get; set; }

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
                ProjectInfo = msg.Payload.ToObject<ProjectInfoContainer>();
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

            if (ProjectInfo == null)
            {
                ProjectInfo = new ProjectInfoContainer();
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
