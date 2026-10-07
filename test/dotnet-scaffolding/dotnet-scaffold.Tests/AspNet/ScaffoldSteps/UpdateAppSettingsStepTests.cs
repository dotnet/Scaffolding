// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using Microsoft.DotNet.Scaffolding.Core.Scaffolders;
using Microsoft.DotNet.Scaffolding.Internal.Services;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using Microsoft.DotNet.Tools.Scaffold.AspNet.ScaffoldSteps.Settings;

namespace Microsoft.DotNet.Tools.Scaffold.AspNet.ScaffoldSteps.Settings.Tests
{
    /// <summary>
    /// Test fixture for UpdateAppSettingsStep that validates Azure AD domain resolution logic.
    /// Issue #3849: Domain must be derived from TenantId, never from Username.
    /// </summary>
    public class UpdateAppSettingsStepTests
    {
        private readonly Mock<ILogger<UpdateAppSettingsStep>> _mockLogger;
        private readonly Mock<IFileSystem> _mockFileSystem;
        private readonly Mock<ITelemetryService> _mockTelemetryService;
        private readonly UpdateAppSettingsStep _step;

        /// <summary>
        /// Helper method to create a mock ScaffolderContext for testing.
        /// </summary>
        private ScaffolderContext CreateMockContext()
        {
            var mockScaffolder = new Mock<IScaffolder>();
            mockScaffolder.Setup(s => s.DisplayName).Returns("Test");
            mockScaffolder.Setup(s => s.Name).Returns("test");
            return new ScaffolderContext(mockScaffolder.Object);
        }

        public UpdateAppSettingsStepTests()
        {
            _mockLogger = new Mock<ILogger<UpdateAppSettingsStep>>();
            _mockFileSystem = new Mock<IFileSystem>();
            _mockTelemetryService = new Mock<ITelemetryService>();
            _mockFileSystem.Setup(fs => fs.DirectoryExists(It.IsAny<string>())).Returns(true);
            _mockFileSystem.Setup(fs => fs.EnumerateFiles(It.IsAny<string>(), It.IsAny<string>(), System.IO.SearchOption.AllDirectories))
                .Returns(new List<string>());

            _step = new UpdateAppSettingsStep(_mockLogger.Object, _mockFileSystem.Object, _mockTelemetryService.Object)
            {
                ProjectPath = "/test/project",
                ClientId = "test-client-id",
                Instance = "https://login.microsoftonline.com/",
                CallbackPath = "/signin-oidc"
            };
        }

        [Fact]
        public async Task ExecuteAsync_DoesNotFabricateDomainForGuidTenantId()
        {
            var tenantGuid = "12345678-1234-1234-1234-123456789012";
            _step.Domain = null;
            _step.TenantId = tenantGuid;
            _step.Username = "user@contoso.onmicrosoft.com";

            var initialContent = new JsonObject { ["AzureAd"] = new JsonObject() };

            _mockFileSystem.Setup(fs => fs.FileExists(It.IsAny<string>())).Returns(false);
            _mockFileSystem.Setup(fs => fs.ReadAllText(It.IsAny<string>())).Returns(initialContent.ToJsonString());

            string? capturedContent = null;
            _mockFileSystem.Setup(fs => fs.WriteAllText(It.IsAny<string>(), It.IsAny<string>()))
                .Callback<string, string>((path, content) => capturedContent = content);

            var result = await _step.ExecuteAsync(CreateMockContext());
            Assert.True(result);
            Assert.NotNull(capturedContent);
            var parsed = JsonNode.Parse(capturedContent);
            Assert.Null(parsed?["AzureAd"]?["Domain"]);
        }

        [Fact]
        public async Task ExecuteAsync_Issue3849_Reproduction_ProducesCorrectDomain()
        {
            _step.Domain = null;
            _step.Username = "user@contoso.onmicrosoft.com";
            _step.TenantId = "contoso.onmicrosoft.com";

            var initialContent = new JsonObject { ["AzureAd"] = new JsonObject() };

            _mockFileSystem.Setup(fs => fs.FileExists(It.IsAny<string>())).Returns(false);
            _mockFileSystem.Setup(fs => fs.ReadAllText(It.IsAny<string>())).Returns(initialContent.ToJsonString());

            string? capturedContent = null;
            _mockFileSystem.Setup(fs => fs.WriteAllText(It.IsAny<string>(), It.IsAny<string>()))
                .Callback<string, string>((path, content) => capturedContent = content);
            var result = await _step.ExecuteAsync(CreateMockContext());
            Assert.True(result);
            Assert.NotNull(capturedContent);
            var parsed = JsonNode.Parse(capturedContent);
            var domain = parsed?["AzureAd"]?["Domain"]?.GetValue<string>();
            Assert.Equal("contoso.onmicrosoft.com", domain);
            var tenantId = parsed?["AzureAd"]?["TenantId"]?.GetValue<string>();
            Assert.Equal("contoso.onmicrosoft.com", tenantId);
            Assert.DoesNotContain("user@contoso", domain);
        }

    }
}
