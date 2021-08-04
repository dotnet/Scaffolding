// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.DotNet.Scaffolding.Shared;
using Microsoft.DotNet.Scaffolding.Shared.Messaging;
using Microsoft.VisualStudio.Web.CodeGeneration.Utils.Messaging;
using Moq;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.VisualStudio.Web.CodeGeneration.Sources.Test
{
    public class MessageHandlerTests
    {
        private Mock<IMessageSender> mockSender = new Mock<IMessageSender>();

        [Fact]
        public void TestMessageHandlerBase_HandleMessage_ThrowsException()
        {
            MockHandler handler = new MockHandler(new MockLogger());

            Assert.Throws<ArgumentNullException>(() => handler.HandleMessage(mockSender.Object, null));
            Assert.Throws<ArgumentNullException>(() => handler.HandleMessage(null, null));
        }

        [Fact]
        public void TestMessageHandlerBase_HandleMessage_ReturnsFalse()
        {
            MockHandler handler = new MockHandler(new MockLogger());
            var message = new Message()
            {
                MessageType = "Invalid"
            };

            Assert.False(handler.HandleMessage(mockSender.Object, message));
        }

        [Fact]
        public void TestMessageHandlerBase_HandleMessage_LogsWarning()
        {
            var mockLogger = new MockLogger();
            MockHandler handler = new MockHandler(mockLogger);
            var message = new Message()
            {
                MessageType = MessageTypes.FileSystemChangeInformation.Value,
                ProtocolVersion = 0
            };

            Assert.True(handler.HandleMessage(mockSender.Object, message));
            Assert.Equal(
                "The protocol version '0' of the message is different than currently handled version '1'.",
                mockLogger.Warnings.First());
        }
    }

    internal class MockHandler : MessageHandlerBase
    {
        private static ISet<string> messageTypesHandled = new HashSet<string>()
        {
            MessageTypes.FileSystemChangeInformation.Value,
            MessageTypes.ProjectInfoRequest.Value,
            MessageTypes.ProjectInfoResponse.Value,
            MessageTypes.Scaffolding_Completed.Value,
            MessageTypes.Terminate.Value
        };

        public MockHandler(ILogger logger) : base(logger)
        {
        }

        public override ISet<string> MessageTypesHandled => messageTypesHandled;

        protected override bool HandleMessageInternal(IMessageSender sender, Message message)
        {
            return true;
        }
    }
}
