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
        const string existingSusi = "existing_signup_policy";

        const string inputDomain = "input domain";
        const string inputTenantId = "input_tenant_Id";
        const string inputClientId = "input_client_Id";
        const string inputInstance = "http://inputInstance/";
        const string inputCallbackPath = "input Callback Path";
        const string inputSusi = "input_signup_policy"; // TODO test input for susi for blazor and webapp

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

            (bool needsUpdate, JObject modifications) = modifier.GetModifiedAzureAdBlock(appSettings, parameters);
            Assert.True(JToken.DeepEquals(expected, modifications));
        }

        [Fact]
        public void ModifyAppSettings_NoInput_DefaultOutput_B2C()
        {
            var modifier = new AppSettingsModifier(new Tool.ProvisioningToolOptions());
            var appSettings = new Newtonsoft.Json.Linq.JObject();
            var parameters = new AuthenticationParameters.ApplicationParameters { IsB2C = true };

            var expected = JToken.FromObject(new
            {
                DefaultProperties.Domain,
                DefaultProperties.TenantId,
                DefaultProperties.ClientId,
                DefaultProperties.Instance,
                DefaultProperties.CallbackPath,
                DefaultProperties.SignUpSignInPolicyId,
                DefaultProperties.SignedOutCallbackPath,
                DefaultProperties.ResetPasswordPolicyId,
                DefaultProperties.EditProfilePolicyId,
                EnablePiiLogging = true
            });

            (bool needsUpdate, JObject modifications) = modifier.GetModifiedAzureAdBlock(appSettings, parameters);
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
                Authority = $"{DefaultProperties.Instance}{DefaultProperties.TenantId}",
                DefaultProperties.ClientId,
                DefaultProperties.ValidateAuthority
            });

            (bool needsUpdate, JObject modifications) = modifier.GetModifiedAzureAdBlock(appSettings, parameters);
            Assert.True(JToken.DeepEquals(expected, modifications));
        }

        [Fact]
        public void ModifyAppSettings_NoInput_BlazorB2C_DefaultOutput()
        {
            var modifier = new AppSettingsModifier(new Tool.ProvisioningToolOptions());
            var appSettings = new Newtonsoft.Json.Linq.JObject();
            var parameters = new AuthenticationParameters.ApplicationParameters { IsBlazorWasm = true, IsB2C = true };

            var expected = JObject.FromObject(new
            {
                Authority = $"{DefaultProperties.Instance}{DefaultProperties.TenantId}/{DefaultProperties.SignUpSignInPolicyId}",
                DefaultProperties.ClientId,
                ValidateAuthority = false
            });

            (bool needsUpdate, JObject modifications) = modifier.GetModifiedAzureAdBlock(appSettings, parameters);
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

            (bool needsUpdate, JObject modifications) = modifier.GetModifiedAzureAdBlock(appSettings, parameters);
            Assert.True(JToken.DeepEquals(expected, modifications));
        }

        [Fact]
        public void ModifyAppSettings_HasInput_NoExistingProperties_B2C()
        {
            var modifier = new AppSettingsModifier(new Tool.ProvisioningToolOptions());
            var appSettings = new Newtonsoft.Json.Linq.JObject();

            var expected = JObject.FromObject(new
            {
                Domain = inputDomain,
                TenantId = inputTenantId,
                ClientId = inputClientId,
                Instance = inputInstance,
                CallbackPath = inputCallbackPath,
                DefaultProperties.SignUpSignInPolicyId,
                DefaultProperties.SignedOutCallbackPath,
                DefaultProperties.ResetPasswordPolicyId,
                DefaultProperties.EditProfilePolicyId,
                EnablePiiLogging = true
            });

            var parameters = new AuthenticationParameters.ApplicationParameters
            {
                Domain = inputDomain,
                TenantId = inputTenantId,
                ClientId = inputClientId,
                Instance = inputInstance,
                CallbackPath = inputCallbackPath,
                IsB2C = true
            };

            (bool needsUpdate, JObject modifications) = modifier.GetModifiedAzureAdBlock(appSettings, parameters);
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

            (bool needsUpdate, JObject modifications) = modifier.GetModifiedAzureAdBlock(appSettings, parameters);
            Assert.True(JToken.DeepEquals(expected, modifications));
        }

        [Fact]
        public void ModifyAppSettings_HasInput_SomePropertiesMissing_InsertDefaults_B2C()
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
                CallbackPath = DefaultProperties.CallbackPath,
                DefaultProperties.SignUpSignInPolicyId,
                DefaultProperties.SignedOutCallbackPath,
                DefaultProperties.ResetPasswordPolicyId,
                DefaultProperties.EditProfilePolicyId,
                EnablePiiLogging = true
            });

            var parameters = new AuthenticationParameters.ApplicationParameters
            {
                Domain = inputDomain,
                TenantId = inputTenantId,
                ClientId = inputClientId,
                IsB2C = true
            };

            (bool needsUpdate, JObject modifications) = modifier.GetModifiedAzureAdBlock(appSettings, parameters);
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

            var expected = JObject.FromObject(new
                {
                    Domain = inputDomain,
                    TenantId = inputTenantId,
                    ClientId = inputClientId,
                    Instance = inputInstance,
                    CallbackPath = inputCallbackPath
                });

            (bool needsUpdate, JObject modifications) = modifier.GetModifiedAzureAdBlock(appSettings, parameters);
            Assert.True(JToken.DeepEquals(expected, modifications));
        }

        [Fact]
        public void ModifyAppSettings_HasAllInputParameters_ExistingPropertiesDiffer_B2C()
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
                        CallbackPath = existingCallbackPath,
                        SignUpSignInPolicyId = existingSusi
                    })
                }
            };

            var parameters = new AuthenticationParameters.ApplicationParameters
            {
                Domain = inputDomain,
                TenantId = inputTenantId,
                ClientId = inputClientId,
                Instance = inputInstance,
                CallbackPath = inputCallbackPath,
                SusiPolicy = inputSusi,
                IsB2C = true
            };

            var expected = JObject.FromObject(new
            {
                Domain = inputDomain,
                TenantId = inputTenantId,
                ClientId = inputClientId,
                Instance = inputInstance,
                CallbackPath = inputCallbackPath,
                SignUpSignInPolicyId = inputSusi,
                SignedOutCallbackPath = DefaultProperties.SignedOutCallbackPath,
                ResetPasswordPolicyId = DefaultProperties.ResetPasswordPolicyId,
                EditProfilePolicyId = DefaultProperties.EditProfilePolicyId,
                EnablePiiLogging = true
        });

            (bool needsUpdate, JObject modifications) = modifier.GetModifiedAzureAdBlock(appSettings, parameters);

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

            (bool needsUpdate, JObject modifications) = modifier.GetModifiedAzureAdBlock(appSettings, parameters);
            Assert.True(JToken.DeepEquals(expected, modifications));
        }

        [Fact]
        public void ModifyAppSettings_HasSomeInputParameters_ExistingPropertiesDiffer_B2C()
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
                IsB2C = true
            };

            var expected = JObject.FromObject(new
            {
                Domain = inputDomain,
                TenantId = existingTenantId,
                ClientId = existingClientId,
                Instance = existingInstance,
                CallbackPath = existingCallbackPath,
                DefaultProperties.SignUpSignInPolicyId,
                DefaultProperties.SignedOutCallbackPath,
                DefaultProperties.ResetPasswordPolicyId,
                DefaultProperties.EditProfilePolicyId,
                EnablePiiLogging = true
            });

            (bool needsUpdate, JObject modifications) = modifier.GetModifiedAzureAdBlock(appSettings, parameters);
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
                ClientId = "",
            };

            var expected = JObject.FromObject(new
            {
                Domain = inputDomain,
                TenantId = existingTenantId,
                ClientId = existingClientId,
                Instance = existingInstance,
                CallbackPath = existingCallbackPath
            });

            (bool needsUpdate, JObject modifications) = modifier.GetModifiedAzureAdBlock(appSettings, parameters);
            Assert.True(JToken.DeepEquals(expected, modifications));
        }

        [Fact]
        public void ModifyAppSettings_HasEmptyInputParameter_ExistingPropertiesNotModified_B2C()
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
                ClientId = "",
                IsB2C = true
            };

            var expected = JObject.FromObject(new
            {
                Domain = inputDomain,
                TenantId = existingTenantId,
                ClientId = existingClientId,
                Instance = existingInstance,
                CallbackPath = existingCallbackPath,
                DefaultProperties.SignUpSignInPolicyId,
                DefaultProperties.SignedOutCallbackPath,
                DefaultProperties.ResetPasswordPolicyId,
                DefaultProperties.EditProfilePolicyId,
                EnablePiiLogging = true
            });

            (bool needsUpdate, JObject modifications) = modifier.GetModifiedAzureAdBlock(appSettings, parameters);
            Assert.True(JToken.DeepEquals(expected, modifications));
        }

        [Fact]
        public void ModifyAppSettings_BlazorWasm_AuthorityIsCorrect()
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
                IsBlazorWasm = true
            };

            var expected = JObject.FromObject(new
            {
                ClientId = inputClientId,
                Authority = $"{inputInstance}{inputTenantId}",
                ValidateAuthority = true
            });

            (bool needsUpdate, JObject modifications) = modifier.GetModifiedAzureAdBlock(appSettings, parameters);
            Assert.True(JToken.DeepEquals(expected, modifications));
        }

        [Fact]
        public void ModifyAppSettings_BlazorWasmB2C_AuthorityIsCorrect()
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
                        CallbackPath = existingCallbackPath,
                        SignUpSignInPolicyId = existingSusi
                    })
                }
            };

            var parameters = new AuthenticationParameters.ApplicationParameters
            {
                Domain = inputDomain,
                TenantId = inputTenantId,
                ClientId = inputClientId,
                Instance = inputInstance,
                IsBlazorWasm = true,
                IsB2C = true
            };

            var expected = JObject.FromObject(new
            {
                ClientId = inputClientId,
                Authority = $"{inputInstance}{inputTenantId}/{existingSusi}",
                ValidateAuthority = false
            });

            (bool needsUpdate, JObject modifications) = modifier.GetModifiedAzureAdBlock(appSettings, parameters);
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
