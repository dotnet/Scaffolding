using System.Collections.Generic;
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

            var expected = JObject.FromObject(DefaultProperties.AzureAd).ToString();

            var modifications = modifier.GetModifiedAppSettings(appSettings, parameters)["AzureAd"].ToString();

            Assert.Equal(expected, modifications);
        }

        [Fact]
        public void ModifyAppSettings_NoInput_Blazor_DefaultOutput()
        {
            var modifier = new AppSettingsModifier(new Tool.ProvisioningToolOptions());
            var appSettings = new Newtonsoft.Json.Linq.JObject();
            var parameters = new AuthenticationParameters.ApplicationParameters { IsBlazorWasm = true };

            var expected = JObject.FromObject(DefaultProperties.AzureAdBlazor).ToString();

            var modifications = modifier.GetModifiedAppSettings(appSettings, parameters)["AzureAd"].ToString();

            Assert.Equal(expected, modifications);
        }


        [Fact]
        public void ModifyAppSettings_HasInput_NoExistingProperties()
        {
            var modifier = new AppSettingsModifier(new Tool.ProvisioningToolOptions());
            var appSettings = new Newtonsoft.Json.Linq.JObject();
            var inputParameters = new Dictionary<string, string>
            {
                { PropertyNames.Domain,  "input domain" },
                { PropertyNames.TenantId, "input tenantId" },
                { PropertyNames.ClientId, "input clientId" },
                { PropertyNames.Instance, "input instance" },
                { PropertyNames.CallbackPath, "input callbackPath" }
            };

            var parameters = new AuthenticationParameters.ApplicationParameters
            {
                Domain = inputParameters[PropertyNames.Domain],
                TenantId = inputParameters[PropertyNames.TenantId],
                ClientId = inputParameters[PropertyNames.ClientId],
                Instance = inputParameters[PropertyNames.Instance],
                CallbackPath = inputParameters[PropertyNames.CallbackPath]
            };

            var expected = JObject.FromObject(inputParameters).ToString();
            var modifications = modifier.GetModifiedAppSettings(appSettings, parameters)["AzureAd"].ToString();

            Assert.Equal(expected, modifications);
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

            var inputParameters = new Dictionary<string, string>
            {
                { PropertyNames.Domain, inputDomain },
                { PropertyNames.TenantId, inputTenantId },
                { PropertyNames.ClientId, inputClientId },
                { PropertyNames.Instance, inputInstance },
                { PropertyNames.CallbackPath, inputCallbackPath }
            };

            var parameters = new AuthenticationParameters.ApplicationParameters
            {
                Domain = inputParameters[PropertyNames.Domain],
                TenantId = inputParameters[PropertyNames.TenantId],
                ClientId = inputParameters[PropertyNames.ClientId],
                Instance = inputParameters[PropertyNames.Instance],
                CallbackPath = inputParameters[PropertyNames.CallbackPath]
            };

            var expected = JObject.FromObject(inputParameters).ToString();
            var modifications = modifier.GetModifiedAppSettings(appSettings, parameters)["AzureAd"].ToString();

            Assert.Equal(expected, modifications);
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
            }).ToString();

            var modifications = modifier.GetModifiedAppSettings(appSettings, parameters)["AzureAd"].ToString();

            Assert.Equal(expected, modifications);
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
            }).ToString();

            var modifications = modifier.GetModifiedAppSettings(appSettings, parameters)["AzureAd"].ToString();

            Assert.Equal(expected, modifications);
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
            var update = AppSettingsModifier.UpdatePropertyIfNecessary(propertyName, existingValue, newValue);

            Assert.Equal(update, expected);
        }
    }
}
