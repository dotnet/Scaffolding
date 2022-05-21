using Microsoft.DotNet.MSIdentity.AuthenticationParameters;
using Microsoft.DotNet.MSIdentity.MicrosoftIdentityPlatform;
using Newtonsoft.Json.Linq;
using Xunit;

namespace Microsoft.DotNet.MSIdentity.UnitTests.Tests
{
    public class AppProvisioningToolTests
    {
        const string existingDomain = "existing domain";
        const string existingTenantId = "existing tenant Id";
        const string existingClientId = "existing client Id";
        const string existingInstance = "existing Instance";
        const string existingCallbackPath = "existing Callback Path";

        const string inputDomain = "input domain";
        const string inputTenantId = "input tenant Id";
        const string inputClientId = "input client Id";
        const string inputInstance = "input Instance";
        const string inputCallbackPath = "input Callback Path";

        [Fact]
        public void ModifyAppSettings_NoInput_DefaultOutput()
        {
            var modifier = new AppSettingsModifier(new Tool.ProvisioningToolOptions());
            var appSettings = new Newtonsoft.Json.Linq.JObject();
            var parameters = new AuthenticationParameters.ApplicationParameters();

            var expected = JToken.FromObject(new
            {
                DefaultProperties.Domain,
                DefaultProperties.TenantId,
                DefaultProperties.ClientId,
                DefaultProperties.Instance,
                DefaultProperties.CallbackPath
            });

            var modifications = modifier.GetModifiedAzureAdBlock(appSettings, parameters);

            Assert.True(JToken.DeepEquals(expected, modifications));
        }

        [Fact]
        public void ModifyAppSettings_NoInput_Blazor_DefaultOutput()
        {
            var modifier = new AppSettingsModifier(new Tool.ProvisioningToolOptions());
            var appSettings = new Newtonsoft.Json.Linq.JObject();
            var parameters = new AuthenticationParameters.ApplicationParameters { IsBlazorWasm = true };

            var expected = JObject.FromObject(new
            {
                DefaultProperties.Authority,
                DefaultProperties.ClientId,
                DefaultProperties.ValidateAuthority
            });

            var modifications = modifier.GetModifiedAzureAdBlock(appSettings, parameters);

            Assert.True(JToken.DeepEquals(expected, modifications));
        }

        [Fact]
        public void ModifyAppSettings_HasInput_NoExistingProperties()
        {
            var modifier = new AppSettingsModifier(new Tool.ProvisioningToolOptions());
            var appSettings = new Newtonsoft.Json.Linq.JObject();

            var expected = JObject.FromObject(new
            {
                Domain = inputDomain,
                TenantId = inputTenantId,
                ClientId = inputClientId,
                Instance = inputInstance,
                CallbackPath = inputCallbackPath
            });

            var parameters = new AuthenticationParameters.ApplicationParameters
            {
                Domain = inputDomain,
                TenantId = inputTenantId,
                ClientId = inputClientId,
                Instance = inputInstance,
                CallbackPath = inputCallbackPath
            };

            var modifications = modifier.GetModifiedAzureAdBlock(appSettings, parameters);

            Assert.True(JToken.DeepEquals(expected, modifications));
        }

        [Fact]
        public void ModifyAppSettings_HasInput_SomePropertiesMissing_InsertDefaults()
        {
            var modifier = new AppSettingsModifier(new Tool.ProvisioningToolOptions());
            var appSettings = new Newtonsoft.Json.Linq.JObject
            {
                {
                    "AzureAd",
                    JToken.FromObject(new
                    {
                        Domain = existingDomain,
                        TenantId = existingTenantId,
                        ClientId = existingClientId
                    })
                }
            };

            var expected = JObject.FromObject(new
            {
                Domain = inputDomain,
                TenantId = inputTenantId,
                ClientId = inputClientId,
                Instance = DefaultProperties.Instance,
                CallbackPath = DefaultProperties.CallbackPath
            });

            var parameters = new AuthenticationParameters.ApplicationParameters
            {
                Domain = inputDomain,
                TenantId = inputTenantId,
                ClientId = inputClientId
            };

            var modifications = modifier.GetModifiedAzureAdBlock(appSettings, parameters);

            Assert.True(JToken.DeepEquals(expected, modifications));
        }

        [Fact]
        public void ModifyAppSettings_HasAllInputParameters_ExistingPropertiesDiffer()
        {
            var modifier = new AppSettingsModifier(new Tool.ProvisioningToolOptions());
            var appSettings = new Newtonsoft.Json.Linq.JObject
            {
                {
                    "AzureAd",
                    JToken.FromObject(new
                    {
                        Domain = existingDomain,
                        TenantId = existingTenantId,
                        ClientId = existingClientId,
                        Instance = existingInstance,
                        CallbackPath = existingCallbackPath
                    })
                }
            };

            var parameters = new AuthenticationParameters.ApplicationParameters
            {
                Domain = inputDomain,
                TenantId = inputTenantId,
                ClientId = inputClientId,
                Instance = inputInstance,
                CallbackPath = inputCallbackPath
            };

            var expected = JObject.FromObject(
                new WebAppSettings
                {
                    Domain = inputDomain,
                    TenantId = inputTenantId,
                    ClientId = inputClientId,
                    Instance = inputInstance,
                    CallbackPath = inputCallbackPath
                });

            var modifications = modifier.GetModifiedAzureAdBlock(appSettings, parameters);

            Assert.True(JToken.DeepEquals(expected, modifications));
        }

        [Fact]
        public void ModifyAppSettings_HasSomeInputParameters_ExistingPropertiesDiffer()
        {
            var modifier = new AppSettingsModifier(new Tool.ProvisioningToolOptions());

            var appSettings = new Newtonsoft.Json.Linq.JObject
            {
                {
                    "AzureAd",
                    JToken.FromObject(new
                    {
                        Domain = existingDomain,
                        TenantId = existingTenantId,
                        ClientId = existingClientId,
                        Instance = existingInstance,
                        CallbackPath = existingCallbackPath
                    })
                }
            };

            var parameters = new AuthenticationParameters.ApplicationParameters
            {
                Domain = inputDomain
            };

            var expected = JObject.FromObject(new
            {
                Domain = inputDomain,
                TenantId = existingTenantId,
                ClientId = existingClientId,
                Instance = existingInstance,
                CallbackPath = existingCallbackPath
            });

            var modifications = modifier.GetModifiedAzureAdBlock(appSettings, parameters);

            Assert.True(JToken.DeepEquals(expected, modifications));
        }

        [Fact]
        public void ModifyAppSettings_HasEmptyInputParameter_ExistingPropertiesNotModified()
        {
            var modifier = new AppSettingsModifier(new Tool.ProvisioningToolOptions());

            var appSettings = new Newtonsoft.Json.Linq.JObject
            {
                {
                    "AzureAd",
                    JToken.FromObject(new
                    {
                        Domain = existingDomain,
                        TenantId = existingTenantId,
                        ClientId = existingClientId,
                        Instance = existingInstance,
                        CallbackPath = existingCallbackPath
                    })
                }
            };

            var parameters = new AuthenticationParameters.ApplicationParameters
            {
                Domain = inputDomain,
                TenantId = "",
                ClientId = ""
            };

            var expected = JObject.FromObject(new
            {
                Domain = inputDomain,
                TenantId = existingTenantId,
                ClientId = existingClientId,
                Instance = existingInstance,
                CallbackPath = existingCallbackPath
            });

            var modifications = modifier.GetModifiedAzureAdBlock(appSettings, parameters);

            Assert.True(JToken.DeepEquals(expected, modifications));
        }

        [Theory]
        [InlineData(PropertyNames.Domain, "", "newValue", "newValue")]
        [InlineData(PropertyNames.Domain, "ExistingDomain", "", null)]
        [InlineData(PropertyNames.Domain, "", "", DefaultProperties.Domain)]
        [InlineData(PropertyNames.Domain, null, "newValue", "newValue")]
        [InlineData(PropertyNames.Domain, "ExistingDomain", null, null)]
        [InlineData(PropertyNames.Domain, null, null, DefaultProperties.Domain)]
        public void UpdatePropertyIfNecessary(string propertyName, string existingValue, string newValue, string expected)
        {
            (bool needsUpdate, JToken update) = AppSettingsModifier.GetUpdatedValue(propertyName, existingValue, newValue);

            Assert.Equal(update?.ToString(), expected);
        }
    }
}
