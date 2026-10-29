// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.DotNet.MSIdentity.AuthenticationParameters;
using Microsoft.DotNet.MSIdentity.MicrosoftIdentityPlatform;
using Microsoft.DotNet.MSIdentity.Tool;
using Microsoft.DotNet.MSIdentity.Shared;
using Microsoft.Graph;
using Microsoft.Graph.Models;
using Moq;
using Xunit;

namespace Microsoft.DotNet.MSIdentity.UnitTests.Tests
{
    public class MicrosoftIdentityPlatformApplicationManagerTests
    {
        [Fact]
        public async Task GetOrCreateSP_MissingResultPreservesExistingFailureReporting()
        {
            using var handler = new RegistrationHandler("servicePrincipal", false, new List<string>());
            using var httpClient = new HttpClient(handler);
            using var graphClient = new GraphServiceClient(httpClient);
            var logger = new Mock<IConsoleLogger>();

            var result = await MicrosoftIdentityPlatformApplicationManager.GetOrCreateSP(
                graphClient, "existing-client", logger.Object);

            Assert.True(handler.FailureReached);
            Assert.Null(result);
            logger.Verify(l => l.LogFailureAndExit(It.IsAny<string>()), Times.Once);
        }

        [Theory]
        [InlineData("create", false)]
        [InlineData("owner", false)]
        [InlineData("servicePrincipal", false)]
        [InlineData("read", false)]
        [InlineData("owner", true)]
        public async Task CreateNewAppAsync_FailureCleansUpOnlyReturnedObject(string failureStage, bool cleanupFails)
        {
            var deletions = new List<string>();
            using var handler = new RegistrationHandler(failureStage, cleanupFails, deletions);
            using var httpClient = new HttpClient(handler);
            using var graphClient = new GraphServiceClient(httpClient);
            var manager = new MicrosoftIdentityPlatformApplicationManager(graphClient);
            var logger = new Mock<IConsoleLogger>();

            var result = await manager.CreateNewAppAsync(null, new ApplicationParameters
            {
                TenantId = "tenant-a",
                ApplicationDisplayName = "test-app"
            }, logger.Object);

            Assert.Null(result);
            Assert.True(handler.FailureReached);
            logger.Verify(l => l.LogFailureAndExit(It.IsAny<string>()), Times.Once);
            if (failureStage == "create")
            {
                Assert.Empty(deletions);
            }
            else
            {
                Assert.EndsWith("/applications/created-object", Assert.Single(deletions));
            }
            if (cleanupFails)
            {
                logger.Verify(l => l.LogJsonMessage(State.Fail, null,
                    It.Is<string>(message => message.Contains("created-client") &&
                        message.Contains("created-object") && message.Contains("tenant-a") &&
                        message.Contains("manually"))), Times.Once);
            }
        }

        [Fact]
        public async Task CreateNewAppAsync_SuccessDoesNotDeleteApplication()
        {
            var deletions = new List<string>();
            using var handler = new RegistrationHandler(null, false, deletions);
            using var httpClient = new HttpClient(handler);
            using var graphClient = new GraphServiceClient(httpClient);
            var logger = new Mock<IConsoleLogger>();

            var result = await new MicrosoftIdentityPlatformApplicationManager(graphClient)
                .CreateNewAppAsync(null, new ApplicationParameters { TenantId = "tenant-a" }, logger.Object);

            Assert.Equal("created-client", result.ClientId);
            Assert.Empty(deletions);
            logger.Verify(l => l.LogFailureAndExit(It.IsAny<string>()), Times.Never);
        }

        private sealed class RegistrationHandler(string failureStage, bool cleanupFails, List<string> deletions) : HttpMessageHandler
        {
            public bool FailureReached { get; private set; }

            protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            {
                var path = request.RequestUri.AbsolutePath;
                if (request.Method == HttpMethod.Delete)
                {
                    deletions.Add(path);
                    if (cleanupFails)
                    {
                        throw new InvalidOperationException("Deletion denied.");
                    }
                    return Task.FromResult(new HttpResponseMessage(HttpStatusCode.NoContent));
                }

                string json;
                if (path.EndsWith("/organization", StringComparison.Ordinal))
                {
                    json = """{"value":[{"id":"tenant-a","tenantType":"AAD"}]}""";
                }
                else if (path.EndsWith("/applications", StringComparison.Ordinal) && request.Method == HttpMethod.Post)
                {
                    if (failureStage == "create")
                    {
                        FailureReached = true;
                        throw new InvalidOperationException("Creation failed.");
                    }
                    json = """{"id":"created-object","appId":"created-client"}""";
                }
                else if (path.EndsWith("/me", StringComparison.Ordinal))
                {
                    if (failureStage == "owner")
                    {
                        FailureReached = true;
                        throw new InvalidOperationException("Owner lookup failed.");
                    }
                    json = """{"id":"user-a"}""";
                }
                else if (path.EndsWith("/owners/$ref", StringComparison.Ordinal))
                {
                    return Task.FromResult(new HttpResponseMessage(HttpStatusCode.NoContent));
                }
                else if (path.EndsWith("/servicePrincipals", StringComparison.Ordinal))
                {
                    if (failureStage == "servicePrincipal")
                    {
                        FailureReached = true;
                        return Task.FromResult(new HttpResponseMessage(HttpStatusCode.NoContent));
                    }
                    json = """{"value":[{"id":"created-sp","appId":"created-client"}]}""";
                }
                else if (path.EndsWith("/applications", StringComparison.Ordinal) && failureStage == "read")
                {
                    FailureReached = true;
                    json = """{"value":[]}""";
                }
                else if (path.EndsWith("/applications", StringComparison.Ordinal) && failureStage is null)
                {
                    json = """{"value":[{"id":"created-object","appId":"created-client"}]}""";
                }
                else
                {
                    throw new InvalidOperationException($"Unexpected request: {request.Method} {path}");
                }
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(json, System.Text.Encoding.UTF8, "application/json")
                });
            }
        }

        [Theory]
        [InlineData("11111111-2222-3333-4444-555555555555", null)]
        [InlineData("contoso.onmicrosoft.com", null)]
        [InlineData("contoso.onmicrosoft.com", "")]
        public void CreateApplication_ExplicitWorkforceTenantWithoutAudience_DefaultsToSingleTenant(string tenantId, string audience)
        {
            var parameters = new ApplicationParameters
            {
                TenantId = tenantId,
                SignInAudience = audience
            };

            var application = MicrosoftIdentityPlatformApplicationManager.CreateApplication(parameters);

            Assert.Equal("AzureADMyOrg", application.SignInAudience);
        }

        [Theory]
        [InlineData("SingleOrg", "AzureADMyOrg")]
        [InlineData("MultiOrg", "AzureADMultipleOrgs")]
        [InlineData("MultiOrgAndPersonal", "AzureADandPersonalMicrosoftAccount")]
        [InlineData("Personal", "PersonalMicrosoftAccount")]
        public void CreateApplication_ExplicitAudience_PreservesRequestedAudience(string audience, string expected)
        {
            var parameters = new ApplicationParameters
            {
                TenantId = "contoso.onmicrosoft.com",
                SignInAudience = audience
            };

            var application = MicrosoftIdentityPlatformApplicationManager.CreateApplication(parameters);

            Assert.Equal(expected, application.SignInAudience);
        }

        [Theory]
        [InlineData(null, false, false, null, "AzureADandPersonalMicrosoftAccount")]
        [InlineData("", false, false, null, "AzureADandPersonalMicrosoftAccount")]
        [InlineData(" ", false, false, null, "AzureADandPersonalMicrosoftAccount")]
        [InlineData("COMMON", false, false, null, "AzureADandPersonalMicrosoftAccount")]
        [InlineData("organizations", false, false, null, "AzureADandPersonalMicrosoftAccount")]
        [InlineData("consumers", false, false, null, "AzureADandPersonalMicrosoftAccount")]
        [InlineData("contoso.onmicrosoft.com", true, false, null, "AzureADandPersonalMicrosoftAccount")]
        [InlineData("contoso.onmicrosoft.com", false, true, null, "AzureADMyOrg")]
        [InlineData("contoso.onmicrosoft.com", false, true, "MultiOrg", "AzureADMyOrg")]
        public void CreateApplication_OutsideWorkforceDefault_PreservesExistingBehavior(
            string tenantId, bool isB2C, bool isCiam, string audience, string expected)
        {
            var parameters = new ApplicationParameters
            {
                TenantId = tenantId,
                IsB2C = isB2C,
                IsCiam = isCiam,
                SignInAudience = audience
            };

            var application = MicrosoftIdentityPlatformApplicationManager.CreateApplication(parameters);

            Assert.Equal(expected, application.SignInAudience);
        }

        [Theory]
        [MemberData(nameof(UriList))]
        public void ValidateUrisTests(List<string> urisToValidate, List<string> validUris)
        {
            var validatedUris = urisToValidate.Where(uri => MicrosoftIdentityPlatformApplicationManager.IsValidUri(uri)).ToList();
            var areEquivalent = (validUris.Count == validatedUris.Count) && !validatedUris.Except(validUris).Any();
            Assert.True(areEquivalent);
        }

        public static IEnumerable<object[]> UriList =>
            new List<object[]>
            {
                new object[]
                {
                    new List<string>
                    {
                        "https://localhost:5001/",
                        "https://localhost:5002/get",
                        "http://localhost:5001/",
                        "https://www.microsoft.com/",
                        "http://www.azure.com",
                        "https://www.testapi.com/get/{id}",
                        "http://www.skype.com",
                        "http://127.0.0.1/get",
                        "http://loopback/post",
                        "badstring",
                        null,
                        string.Empty,
                        ""
                    },

                    new List<string>
                    {
                        "https://localhost:5001/",
                        "https://localhost:5002/get",
                        "http://localhost:5001/",
                        "https://www.microsoft.com/",
                        "https://www.testapi.com/get/{id}",
                        "http://127.0.0.1/get",
                        "http://loopback/post",
                    }
                }
            };

        [Fact]
        public void PreAuthorizeBlazorWasmClientAppTest_WhenBlazorWasmClientAppIdEmpty_ReturnFalse()
        {
            var originalApp = new Application { Api = new ApiApplication() };
            var toolOptions = new ProvisioningToolOptions
            {
                BlazorWasmClientAppId = ""
            };
            var output = MicrosoftIdentityPlatformApplicationManager.PreAuthorizeBlazorWasmClientApp(originalApp, toolOptions, null);
            Assert.False(output);
            Assert.Null(originalApp.Api.PreAuthorizedApplications);
        }

        [Fact]
        public void PreAuthorizeBlazorWasmClientAppTest_WhenExistingAppHasNoPermissionScopes_ReturnFalse()
        {
            var originalApp = new Application { Api = new ApiApplication() };
            var toolOptions = new ProvisioningToolOptions
            {
                BlazorWasmClientAppId = "id"
            };

            var output = MicrosoftIdentityPlatformApplicationManager.PreAuthorizeBlazorWasmClientApp(originalApp, toolOptions, null);
            Assert.False(output);
            Assert.Null(originalApp.Api.PreAuthorizedApplications);
        }

        [Fact]
        public void PreAuthorizeBlazorWasmClientAppTest_WhenExistingAppHasMatchingPreAuthorizedApplications_ReturnFalse()
        {
            var clientId = "id";
            var permissionId = "permissionId";

            var originalApp = new Application
            {
                Api = new ApiApplication
                {
                    PreAuthorizedApplications = new List<PreAuthorizedApplication>
                    {
                        new PreAuthorizedApplication
                        {
                            AppId = clientId,
                            DelegatedPermissionIds = new List<string>
                            {
                                permissionId
                            }
                        }
                    }
                }
            };

            var toolOptions = new ProvisioningToolOptions
            {
                BlazorWasmClientAppId = clientId
            };

            var output = MicrosoftIdentityPlatformApplicationManager.PreAuthorizeBlazorWasmClientApp(originalApp, toolOptions, null);
            Assert.False(output);
            Assert.Equal(originalApp.Api.PreAuthorizedApplications.First().AppId, clientId);
            Assert.Equal(originalApp.Api.PreAuthorizedApplications.First().DelegatedPermissionIds.First(), permissionId);
        }

        [Fact]
        public void PreAuthorizeBlazorWasmClientAppTest_NoMatchingPreAuthorizedApplications_ReturnTrueAndUpdate()
        {
            var clientId = "id";
            var permissionId = Guid.NewGuid();

            var originalApp = new Application
            {
                Api = new ApiApplication
                {
                    Oauth2PermissionScopes = new List<PermissionScope>
                    {
                        new PermissionScope
                        {
                            Id = permissionId
                        }
                    },
                    PreAuthorizedApplications = new List<PreAuthorizedApplication>
                    {
                        new PreAuthorizedApplication
                        {
                            AppId = "existingClientId",
                            DelegatedPermissionIds = new List<string>
                            {
                                "existingPermissionId"
                            }
                        }
                    }
                }
            };

            var updatedApp = new Application();

            var toolOptions = new ProvisioningToolOptions
            {
                BlazorWasmClientAppId = clientId
            };

            var output = MicrosoftIdentityPlatformApplicationManager.PreAuthorizeBlazorWasmClientApp(originalApp, toolOptions, updatedApp);

            Assert.True(output);
            // Preauthorized application should be added, not replaced
            Assert.Equal(2, updatedApp.Api.PreAuthorizedApplications.Count());
            Assert.Contains(updatedApp.Api.PreAuthorizedApplications,
                app => app.AppId.Equals(clientId)
                && app.DelegatedPermissionIds.Any(
                    id => id.ToString().Equals(permissionId.ToString())));
        }

        [Fact]
        public void UpdateImplicitGrantSettingsTest_WhenBlazorWasm_SetCheckboxesTrue()
        {
            var originalApp = new Application
            {
                Web = new WebApplication
                {
                    ImplicitGrantSettings = new ImplicitGrantSettings
                    {
                        EnableAccessTokenIssuance = false,
                        EnableIdTokenIssuance = false
                    }
                }
            };
            var toolOptions = new ProvisioningToolOptions
            {
                ProjectType = "blazorwasm"
            };

            var output = MicrosoftIdentityPlatformApplicationManager.UpdateImplicitGrantSettings(originalApp, toolOptions);
            Assert.True(output);
            Assert.True(originalApp.Web.ImplicitGrantSettings.EnableAccessTokenIssuance);
            Assert.True(originalApp.Web.ImplicitGrantSettings.EnableIdTokenIssuance);
        }

        [Theory]
        [InlineData(false, false, false, false, false)]
        [InlineData(true, true, true, true, false)]
        [InlineData(false, false, true, true, false)]
        [InlineData(true, true, false, false, false)]
        [InlineData(false, true, false, true, true)]
        [InlineData(false, true, false, false, true)]
        [InlineData(false, false, true, false, true)]
        [InlineData(false, false, false, true, true)]
        [InlineData(true, false, true, false, true)]
        [InlineData(true, false, true, true, true)]
        [InlineData(true, true, false, true, true)]
        [InlineData(true, true, true, false, true)]
        public void UpdateImplicitGrantSettingsTest_SetCheckboxes(bool appAccessToken, bool toolAccessToken, bool appIdToken, bool toolIdToken, bool expected)
        {
            var originalApp = new Application
            {
                Web = new WebApplication
                {
                    ImplicitGrantSettings = new ImplicitGrantSettings
                    {
                        EnableAccessTokenIssuance = appAccessToken,
                        EnableIdTokenIssuance = appIdToken
                    }
                }
            };

            var toolOptions = new ProvisioningToolOptions
            {
                EnableAccessToken = toolAccessToken,
                EnableIdToken = toolIdToken
            };

            var needsUpdate = MicrosoftIdentityPlatformApplicationManager.UpdateImplicitGrantSettings(originalApp, toolOptions);
            Assert.Equal(expected, needsUpdate);
            Assert.Equal(originalApp.Web.ImplicitGrantSettings.EnableAccessTokenIssuance, toolAccessToken);
            Assert.Equal(originalApp.Web.ImplicitGrantSettings.EnableIdTokenIssuance, toolIdToken);
        }
    }
}
