// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using Microsoft.DotNet.Scaffolding.Shared;
using Microsoft.DotNet.Scaffolding.Shared.Messaging;
using Microsoft.DotNet.Scaffolding.Shared.ProjectModel;

namespace Microsoft.VisualStudio.Web.CodeGeneration.Design
{
    internal class MessageOrchestrator
    {
        private ScaffoldingClient _client;
        private ILogger _logger;
        private ProjectInformationMessageHandler _projectInformationMessageHandler;
        private int _currentProtocolVersion;

        private int _serverProtocolVersion = 1;
        private string _serverHostId = string.Empty;

        private bool SupportsFileSystemChangeMessages => _serverProtocolVersion >= MessageTypes.FileSystemChangeInformation.MinProtocolVersion;

        public MessageOrchestrator(ScaffoldingClient client, ILogger logger)
        {
            if (client == null)
            {
                throw new ArgumentNullException(nameof(client));
            }
            if (logger == null)
            {
                throw new ArgumentNullException(nameof(logger));
            }

            _logger = logger;
            _client = client;
            _projectInformationMessageHandler = new ProjectInformationMessageHandler(_logger);
            _currentProtocolVersion = _projectInformationMessageHandler.CurrentProtocolVersion;
            client.AddHandler(_projectInformationMessageHandler);
        }

        public IProjectContext GetProjectInformation()
        {
            var message = _client.CreateMessage(MessageTypes.ProjectInfoRequest, null, _projectInformationMessageHandler.CurrentProtocolVersion);

            _client.Send(message);
            // Read the project Information
            var responseMessage = _client.ReadMessage();
            _serverProtocolVersion = responseMessage.ProtocolVersion;
            _serverHostId = responseMessage.HostId;

            var projectInfo = _projectInformationMessageHandler.ProjectInformation;

            if (projectInfo == null)
            {
                throw new InvalidOperationException(Resources.ProjectInformationError);
            }

            return projectInfo;
        }

        public void SendFileSystemChangeInformation(IEnumerable<FileSystemChangeInformation> fileSystemChanges)
        {
            if (!SupportsFileSystemChangeMessages)
            {
                _logger.LogMessage(string.Format(Resources.FileSystemChangeMessageNotSupported, _serverHostId, _serverProtocolVersion));
                _logger.LogMessage(Resources.RunWithoutSimulationMode);
                return;
            }

            if (fileSystemChanges != null)
            {
                foreach (var fileSystemChange in fileSystemChanges)
                {
                    var message = _client.CreateMessage(MessageTypes.FileSystemChangeInformation, fileSystemChange, _currentProtocolVersion);
                    _client.Send(message);
                }
            }
        }

        internal void SendScaffoldingCompletedMessage()
        {
            var message = _client.CreateMessage(MessageTypes.Scaffolding_Completed, null, _currentProtocolVersion);
            _client.Send(message);
        }
    }
}
