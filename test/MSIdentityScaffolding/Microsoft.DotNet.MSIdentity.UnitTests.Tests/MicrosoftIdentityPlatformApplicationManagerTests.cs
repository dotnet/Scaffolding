using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.DotNet.MSIdentity.MicrosoftIdentityPlatformApplication;
using Microsoft.DotNet.MSIdentity.Tool;
using Microsoft.Graph;
using Xunit;

namespace Microsoft.DotNet.MSIdentity.UnitTests.Tests
{
    public class MicrosoftIdentityPlatformApplicationManagerTests
    {
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
            var originalApp = new Graph.Application { Api = new Graph.ApiApplication() };
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
            var originalApp = new Graph.Application { Api = new Graph.ApiApplication() };
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

            var originalApp = new Graph.Application {
                Api = new Graph.ApiApplication
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

            var originalApp = new Graph.Application
            {
                Api = new Graph.ApiApplication
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

            var updatedApp = new Graph.Application();

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
        public void UpdateImplicitGrantSettingsTest_WhenBlazorWasm_SetCheckboxesFalse()
        {
            var originalApp = new Graph.Application
            {
                Web = new WebApplication
                {
                    ImplicitGrantSettings = new ImplicitGrantSettings
                    {
                        EnableAccessTokenIssuance = true,
                        EnableIdTokenIssuance = true
                    }
                }
            };
            var toolOptions = new ProvisioningToolOptions
            {
                ProjectType = "blazorwasm"
            };

            var output = MicrosoftIdentityPlatformApplicationManager.UpdateImplicitGrantSettings(originalApp, toolOptions); // TODO unit tests
            Assert.True(output);
            Assert.False(originalApp.Web.ImplicitGrantSettings.EnableAccessTokenIssuance);
            Assert.False(originalApp.Web.ImplicitGrantSettings.EnableIdTokenIssuance);
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
            var originalApp = new Graph.Application
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
