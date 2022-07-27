using System;
using Newtonsoft.Json.Linq;

namespace Microsoft.DotNet.MSIdentity.AuthenticationParameters
{
    public static class PropertyNames
    {
        public const string Domain = nameof(Domain);
        public const string TenantId = nameof(TenantId);
        public const string ClientId = nameof(ClientId);
        public const string ClientSecret = nameof(ClientSecret);
        public const string ClientCertificates = nameof(ClientCertificates);
        public const string CallbackPath = nameof(CallbackPath);
        public const string Instance = nameof(Instance);
        public const string Authority = nameof(Authority);
        public const string ValidateAuthority = nameof(ValidateAuthority);
        public const string BaseUrl = nameof(BaseUrl);
        public const string Scopes = nameof(Scopes);
        public const string SignUpSignInPolicyId = nameof(SignUpSignInPolicyId);
        public const string ResetPasswordPolicyId = nameof(ResetPasswordPolicyId);
        public const string EditProfilePolicyId = nameof(EditProfilePolicyId);
        public const string SignedOutCallbackPath = nameof(SignedOutCallbackPath);
    }

    // getting default properties from
    // https://github.com/dotnet/aspnetcore/blob/6bc4b79f4ee7af00edcbb435e5ee4c1de349a110/src/ProjectTemplates/Web.ProjectTemplates/content/StarterWeb-CSharp/appsettings.json
    public static class DefaultProperties
    {
        public const string Domain = "qualified.domain.name";
        public const string TenantId = "22222222-2222-2222-2222-222222222222";
        public const string ClientId = "11111111-1111-1111-11111111111111111";
        public const string Instance = "https://login.microsoftonline.com/";
        public const string CallbackPath = "/signin-oidc";
        public const string ClientSecret = "Client secret from app-registration. Check user secrets/azure portal.";
        public const bool ValidateAuthority = true;

        // B2C properties 
        public const string SignUpSignInPolicyId = "b2c_1_susi";
        public const string ResetPasswordPolicyId = "b2c_1_reset";
        public const string EditProfilePolicyId = "b2c_1_edit_profile";
        public const string SignedOutCallbackPath = "/signout/B2C_1_susi";

        public const string MicrosoftGraphBaseUrl = "https://graph.microsoft.com/v1.0";
        public const string MicrosoftGraphScopes = "user.read";
        public const string ApiScopes = "access_as_user";
    }

    public class AzureAdBlock
    {
        public bool IsBlazorWasm;
        public bool IsWebApi;
        public bool IsB2C;

        public string? ClientId;
        public string? Instance = DefaultProperties.Instance;
        public string? Domain;
        public string? TenantId;
        public string? Authority;
        public string? CallbackPath = DefaultProperties.CallbackPath;
        public string? SignUpSignInPolicyId = DefaultProperties.SignUpSignInPolicyId;
        public string? ResetPasswordPolicyId = DefaultProperties.ResetPasswordPolicyId;
        public string? EditProfilePolicyId = DefaultProperties.EditProfilePolicyId;
        public string? SignedOutCallbackPath = DefaultProperties.SignedOutCallbackPath;

        public string? Scopes;

        public string? ClientSecret;
        public string[]? ClientCertificates;

        public AzureAdBlock(ApplicationParameters applicationParameters, JObject? existingBlock = null)
        {
            IsBlazorWasm = applicationParameters.IsBlazorWasm;
            IsWebApi = applicationParameters.IsWebApi.GetValueOrDefault();
            IsB2C = applicationParameters.IsB2C;

            Domain = !string.IsNullOrEmpty(applicationParameters.Domain) ? applicationParameters.Domain : existingBlock?.GetValue(PropertyNames.Domain)?.ToString() ?? DefaultProperties.Domain;
            TenantId = !string.IsNullOrEmpty(applicationParameters.TenantId) ? applicationParameters.TenantId : existingBlock?.GetValue(PropertyNames.TenantId)?.ToString() ?? DefaultProperties.TenantId;
            ClientId = !string.IsNullOrEmpty(applicationParameters.ClientId) ? applicationParameters.ClientId : existingBlock?.GetValue(PropertyNames.ClientId)?.ToString() ?? DefaultProperties.ClientId;
            Instance = !string.IsNullOrEmpty(applicationParameters.Instance) ? applicationParameters.Instance : existingBlock?.GetValue(PropertyNames.Instance)?.ToString() ?? DefaultProperties.Instance;
            CallbackPath = !string.IsNullOrEmpty(applicationParameters.CallbackPath) ? applicationParameters.CallbackPath : existingBlock?.GetValue(PropertyNames.CallbackPath)?.ToString() ?? DefaultProperties.CallbackPath;
            Authority = !string.IsNullOrEmpty(applicationParameters.Authority) ? applicationParameters.Authority : existingBlock?.GetValue(PropertyNames.Authority)?.ToString();
            Scopes = !string.IsNullOrEmpty(applicationParameters.CalledApiScopes) ? applicationParameters.CalledApiScopes : existingBlock?.GetValue(PropertyNames.Scopes)?.ToString()
                ?? (applicationParameters.CallsDownstreamApi ? DefaultProperties.ApiScopes : applicationParameters.CallsMicrosoftGraph ? DefaultProperties.MicrosoftGraphScopes : null);
            SignUpSignInPolicyId = !string.IsNullOrEmpty(applicationParameters.SusiPolicy) ? applicationParameters.SusiPolicy : existingBlock?.GetValue(PropertyNames.SignUpSignInPolicyId)?.ToString() ?? DefaultProperties.SignUpSignInPolicyId;

            ClientSecret = existingBlock?.GetValue(PropertyNames.ClientSecret)?.ToString() ?? DefaultProperties.ClientSecret;
            ClientCertificates = existingBlock?.GetValue(PropertyNames.ClientCertificates)?.ToObject<string[]>();
        }

        public dynamic BlazorSettings => new
        {
            ClientId = ClientId ?? DefaultProperties.ClientId, // here, if a value is null, we could use the default properties
            Authority = Authority ?? (IsB2C ? $"{Instance}{TenantId}/{SignUpSignInPolicyId}" : $"{Instance}{TenantId}"),
            ValidateAuthority = !IsB2C
        };

        public dynamic WebAppSettings => new
        {
            Instance = Instance ?? DefaultProperties.Instance,
            Domain = Domain ?? DefaultProperties.Domain,
            TenantId = TenantId ?? DefaultProperties.TenantId,
            ClientId = ClientId ?? DefaultProperties.ClientId,
            CallbackPath = CallbackPath ?? DefaultProperties.CallbackPath
        };

        public dynamic WebApiSettings => new
        {
            Instance = Instance ?? DefaultProperties.Instance,
            Domain = Domain ?? DefaultProperties.Domain,
            TenantId = TenantId ?? DefaultProperties.TenantId,
            ClientId = ClientId ?? DefaultProperties.ClientId,
            CallbackPath = CallbackPath ?? DefaultProperties.CallbackPath,
            Scopes = Scopes ?? DefaultProperties.ApiScopes,
            ClientSecret = ClientSecret ?? DefaultProperties.ClientSecret,
            ClientCertificates = ClientCertificates ?? Array.Empty<string>()
        };

        public dynamic B2CSettings => new
        {
            SignUpSignInPolicyId = SignUpSignInPolicyId ?? DefaultProperties.SignUpSignInPolicyId,
            SignedOutCallbackPath = SignedOutCallbackPath ?? DefaultProperties.SignedOutCallbackPath,
            ResetPasswordPolicyId = ResetPasswordPolicyId ?? DefaultProperties.ResetPasswordPolicyId,
            EditProfilePolicyId = EditProfilePolicyId ?? DefaultProperties.EditProfilePolicyId,
            EnablePiiLogging = true
        };

        public JObject ToJObject()
        {
            if (IsBlazorWasm)
            {
                return JObject.FromObject(BlazorSettings);
            }

            var jObject = IsWebApi ? JObject.FromObject(WebApiSettings) : JObject.FromObject(WebAppSettings);

            if (IsB2C)
            {
                jObject.Merge(JObject.FromObject(B2CSettings));
            }

            return jObject;
        }
    }

    public class ApiSettingsBlock
    {
        public string? BaseUrl;
        public string? Scopes;
    }
}
