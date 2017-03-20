// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.VisualStudio.Web.CodeGeneration.Contracts.Messaging;
using Microsoft.VisualStudio.Web.CodeGeneration.Utils.Messaging;
using Xunit;

namespace Microsoft.VisualStudio.Web.CodeGeneration.Sources.Test
{
    public class FileSystemChangeMessageHandlerTests
    {
        [Fact]
        public void TestFileSystemChangeMessageHandler_MessageTypes()
        {
            FileSystemChangeMessageHandler handler = new FileSystemChangeMessageHandler(new MockLogger());
            Assert.Equal(1, handler.MessageTypesHandled.Count);
            Assert.Contains(MessageTypes.FileSystemChangeInformation.Value, handler.MessageTypesHandled);
        }
    }
}
