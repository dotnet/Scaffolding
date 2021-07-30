// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.
using Microsoft.DotNet.Scaffolding.Shared;
using Microsoft.DotNet.Scaffolding.Shared.Messaging;
using Microsoft.DotNet.Scaffolding.Shared.ProjectModel;
using Microsoft.VisualStudio.Web.CodeGeneration.Utils.Messaging;
using Moq;
using Newtonsoft.Json.Linq;
using Xunit;

namespace Microsoft.VisualStudio.Web.CodeGeneration.Sources.Test
{
    public class ProjectInformationMessageHandlerTests
    {
        private Mock<IMessageSender> mockSender = new Mock<IMessageSender>(MockBehavior.Strict);
        [Fact]
        public void TestProjectInformationMessageHandler_MessageTypes()
        {
            var handler = new ProjectInformationMessageHandler(new MockLogger());
            Assert.Equal(2, handler.MessageTypesHandled.Count);

            Assert.Contains(MessageTypes.ProjectInfoRequest.Value, handler.MessageTypesHandled);
            Assert.Contains(MessageTypes.ProjectInfoResponse.Value, handler.MessageTypesHandled);
        }

        [Fact]
        public void TestProjectInformationMessageHandler_RequestMessage()
        {
            var projectContext = new CommonProjectContext()
            {
                AssemblyName = "dummy",
                AssemblyFullPath = "dummyPath"
            };

            var handler = new ProjectInformationMessageHandler(projectContext, new MockLogger());
            var message = new Message()
            {
                MessageType = MessageTypes.ProjectInfoRequest.Value,
                Payload = null,
                HostId = "test",
                ProtocolVersion = 1
            };

            var responseMessage = new Message()
            {
                MessageType = MessageTypes.ProjectInfoResponse.Value,
                HostId = "testClient",
                Payload = JToken.FromObject(projectContext),
                ProtocolVersion = 1
            };

            mockSender.Setup(s => s.CreateMessage(MessageTypes.ProjectInfoResponse, projectContext, 1)).Returns(responseMessage);
            mockSender.Setup(s => s.Send(responseMessage)).Returns(true);
            handler.HandleMessage(mockSender.Object, message);

            mockSender.VerifyAll();
        }
    }
}
