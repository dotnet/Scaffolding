// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

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
    /// Validates that Azure AD settings do not generate Domain and preserve intentional values.
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
                ProjectPath = Path.Combine(Path.GetTempPath(), "UpdateAppSettingsStepTests", "TestApp.csproj"),
                ClientId = "test-client-id",
                Instance = "https://login.microsoftonline.com/",
                CallbackPath = "/signin-oidc"
            };
        }

        [Theory]
        [InlineData("12345678-1234-1234-1234-123456789012")]
        [InlineData("contoso.onmicrosoft.com")]
        [InlineData("contoso.com")]
        public async Task ExecuteAsync_DoesNotGenerateDomain(string tenantId)
        {
            _step.TenantId = tenantId;
            _step.Username = "user@contoso.onmicrosoft.com";

            var azureAd = await ExecuteAndReadAzureAdAsync();
            Assert.False(azureAd.ContainsKey("Domain"));
            Assert.Equal(tenantId, azureAd["TenantId"]?.GetValue<string>());
        }

        [Fact]
        public async Task ExecuteAsync_Issue3849_Reproduction_OmitsDomain()
        {
            _step.Username = "user@contoso.onmicrosoft.com";
            _step.TenantId = "contoso.onmicrosoft.com";

            var azureAd = await ExecuteAndReadAzureAdAsync(new JsonObject());
            Assert.False(azureAd.ContainsKey("Domain"));
            Assert.Equal("contoso.onmicrosoft.com", azureAd["TenantId"]?.GetValue<string>());
        }

        [Theory]
        [InlineData("12345678-1234-1234-1234-123456789012", "contoso.onmicrosoft.com")]
        [InlineData("contoso.onmicrosoft.com", "fabrikam.onmicrosoft.com")]
        [InlineData("12345678-1234-1234-1234-123456789012", "login.contoso.com")]
        [InlineData("contoso.onmicrosoft.com", "login.contoso.com")]
        [InlineData(null, "contoso.onmicrosoft.com")]
        [InlineData("12345678-1234-1234-1234-123456789012", "another@contoso.onmicrosoft.com.onmicrosoft.com")]
        public async Task ExecuteAsync_PreservesExistingDomain(string? tenantId, string domain)
        {
            _step.Username = "user@contoso.onmicrosoft.com";
            _step.TenantId = tenantId;

            var azureAd = await ExecuteAndReadAzureAdAsync(new JsonObject
            {
                ["Domain"] = domain,
                ["SignUpSignInPolicyId"] = "B2C_1_signupsignin"
            });

            Assert.Equal(domain, azureAd["Domain"]?.GetValue<string>());
            Assert.Equal("B2C_1_signupsignin", azureAd["SignUpSignInPolicyId"]?.GetValue<string>());
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("contoso")]
        public async Task ExecuteAsync_PreservesExistingDomainWithoutEmailUsername(string? username)
        {
            _step.Username = username;
            _step.TenantId = "12345678-1234-1234-1234-123456789012";

            var azureAd = await ExecuteAndReadAzureAdAsync(new JsonObject
            {
                ["Domain"] = "contoso.onmicrosoft.com"
            });

            Assert.Equal("contoso.onmicrosoft.com", azureAd["Domain"]?.GetValue<string>());
        }

        [Theory]
        [InlineData("12345678-1234-1234-1234-123456789012", null)]
        [InlineData("contoso.onmicrosoft.com", null)]
        [InlineData("contoso.onmicrosoft.com", "")]
        [InlineData(null, null)]
        public async Task ExecuteAsync_RemovesLegacyUsernameDerivedDomain(string? tenantId, string? explicitDomain)
        {
            _step.Username = "user@contoso.onmicrosoft.com";
            _step.TenantId = tenantId;
            _step.Domain = explicitDomain;

            var azureAd = await ExecuteAndReadAzureAdAsync(new JsonObject
            {
                ["Domain"] = "user@contoso.onmicrosoft.com.onmicrosoft.com"
            });

            Assert.False(azureAd.ContainsKey("Domain"));
        }

        [Theory]
        [InlineData(null, "contoso.onmicrosoft.com")]
        [InlineData("contoso.onmicrosoft.com", "contoso.onmicrosoft.com")]
        [InlineData("fabrikam.onmicrosoft.com", "contoso.onmicrosoft.com")]
        [InlineData("user@contoso.onmicrosoft.com.onmicrosoft.com", "contoso.onmicrosoft.com")]
        [InlineData("user@contoso.onmicrosoft.com.onmicrosoft.com", "user@contoso.onmicrosoft.com.onmicrosoft.com")]
        public async Task ExecuteAsync_HonorsExplicitDomain(string? existingDomain, string explicitDomain)
        {
            _step.Username = "user@contoso.onmicrosoft.com";
            _step.TenantId = "12345678-1234-1234-1234-123456789012";
            _step.Domain = explicitDomain;

            var azureAd = await ExecuteAndReadAzureAdAsync(new JsonObject
            {
                ["Domain"] = existingDomain
            });

            Assert.Equal(explicitDomain, azureAd["Domain"]?.GetValue<string>());
        }

        private async Task<JsonObject> ExecuteAndReadAzureAdAsync(JsonObject? existingAzureAd = null)
        {
            if (existingAzureAd is not null)
            {
                var appSettingsPath = Path.Combine(Path.GetTempPath(), "UpdateAppSettingsStepTests", "appsettings.json");
                var initialContent = new JsonObject { ["AzureAd"] = existingAzureAd };
                _mockFileSystem.Setup(fs => fs.EnumerateFiles(It.IsAny<string>(), "appsettings.json", SearchOption.AllDirectories))
                    .Returns(new[] { appSettingsPath });
                _mockFileSystem.Setup(fs => fs.FileExists(appSettingsPath)).Returns(true);
                _mockFileSystem.Setup(fs => fs.ReadAllText(appSettingsPath)).Returns(initialContent.ToJsonString());
            }

            string? capturedContent = null;
            _mockFileSystem.Setup(fs => fs.WriteAllText(It.IsAny<string>(), It.IsAny<string>()))
                .Callback<string, string>((path, content) => capturedContent = content);

            var result = await _step.ExecuteAsync(CreateMockContext());
            Assert.True(result);
            Assert.NotNull(capturedContent);
            var azureAd = Assert.IsType<JsonObject>(JsonNode.Parse(capturedContent)?["AzureAd"]);
            Assert.Equal(_step.ClientId, azureAd["ClientId"]?.GetValue<string>());
            Assert.Equal(_step.Instance, azureAd["Instance"]?.GetValue<string>());
            Assert.Equal(_step.CallbackPath, azureAd["CallbackPath"]?.GetValue<string>());
            return azureAd;
        }
    }
}
