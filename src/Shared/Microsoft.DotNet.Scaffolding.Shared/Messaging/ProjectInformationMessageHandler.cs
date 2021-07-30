// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Diagnostics.CodeAnalysis;
using System.Collections.Generic;
using Microsoft.DotNet.Scaffolding.Shared.Messaging;
using Microsoft.DotNet.Scaffolding.Shared.ProjectModel;
using Microsoft.DotNet.Scaffolding.Shared;

namespace Microsoft.DotNet.Scaffolding.Shared.Messaging
{
    public class ProjectInformationMessageHandler : MessageHandlerBase
    {
        private HashSet<string> _messageTypesHandled = new HashSet<string>()
        {
            MessageTypes.ProjectInfoRequest.Value,
            MessageTypes.ProjectInfoResponse.Value
        };

        private IProjectContext _projectInformation;

        public ProjectInformationMessageHandler(IProjectContext projectInformation, ILogger logger)
            : base(logger)
        {
            if (projectInformation == null)
            {
                throw new ArgumentNullException(nameof(projectInformation));
            }

            _projectInformation = projectInformation;
        }

        public ProjectInformationMessageHandler(ILogger logger)
            : base(logger)
        {

        }

        public IProjectContext ProjectInformation
        {
            get
            {
                return _projectInformation;
            }
        }

        public override ISet<string> MessageTypesHandled => _messageTypesHandled;

        protected override bool HandleMessageInternal(IMessageSender sender, Message message)
        {
            if (MessageTypes.ProjectInfoRequest.Value.Equals(message.MessageType, StringComparison.OrdinalIgnoreCase))
            {
                Message response = sender.CreateMessage(MessageTypes.ProjectInfoResponse, _projectInformation, CurrentProtocolVersion);
                sender.Send(response);
            }
            else if (MessageTypes.ProjectInfoResponse.Value.Equals(message.MessageType, StringComparison.OrdinalIgnoreCase))
            {
                BuildProjectInformation(message);
            }
            else
            {
                return false;
            }

            return true;
        }

        [SuppressMessage("supressing re-throw exception", "CA2200")]
        private void BuildProjectInformation(Message msg)
        {
            try
            {
                _projectInformation = msg.Payload.ToObject<CommonProjectContext>();
            }
            catch (Exception ex)
            {
                Logger.LogMessage($"{MessageStrings.InvalidProjectInformationMessage}{Environment.NewLine}{msg?.ToString()}");
                throw ex;
            }
        }
    }
}
