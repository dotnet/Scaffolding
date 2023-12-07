// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.DotNet.Scaffolding.Shared.Messaging;
using Xunit;

namespace Microsoft.VisualStudio.Web.CodeGeneration.Sources.Test
{
    public class FileSystemChangeMessageHandlerTests
    {
        [Fact]
        public void TestFileSystemChangeMessageHandler_MessageTypes()
        {
            FileSystemChangeMessageHandler handler = new FileSystemChangeMessageHandler(new MockLogger());
            Assert.Single(handler.MessageTypesHandled);
            Assert.Contains(MessageTypes.FileSystemChangeInformation.Value, handler.MessageTypesHandled);
        }
    }
}
