using System;
using Newtonsoft.Json.Linq;

namespace Microsoft.DotNet.MSIdentity.AuthenticationParameters
{
    public class PropertyNames
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

        public const string Authority = "https://login.microsoftonline.com/22222222-2222-2222-2222-222222222222";
        public const bool ValidateAuthority = true;

        public const string MicrosoftGraphBaseUrl = "https://graph.microsoft.com/v1.0";
        public const string MicrosoftGraphScopes = "user.read";
        public const string ApiScopes = "access_as_user";
    }

    public class AzureAdBlock
    {
        public bool IsBlazorWasm;
        public bool IsWebApi;

        public string? ClientId;
        public string? Instance = DefaultProperties.Instance;
        public string? Domain;
        public string? TenantId;
        public string? Authority;
        public string? CallbackPath = DefaultProperties.CallbackPath;

        public string? Scopes;

        public string? ClientSecret = DefaultProperties.ClientSecret;
        public string[]? ClientCertificates;

        public AzureAdBlock(ApplicationParameters applicationParameters)
        {
            IsBlazorWasm = applicationParameters.IsBlazorWasm;
            IsWebApi = applicationParameters.IsWebApi.GetValueOrDefault();

            Domain = !string.IsNullOrEmpty(applicationParameters.Domain) ? applicationParameters.Domain : null;
            TenantId = !string.IsNullOrEmpty(applicationParameters.TenantId) ? applicationParameters.TenantId : null;
            ClientId = !string.IsNullOrEmpty(applicationParameters.ClientId) ? applicationParameters.ClientId : null;
            Instance = !string.IsNullOrEmpty(applicationParameters.Instance) ? applicationParameters.Instance : null;
            Authority = !string.IsNullOrEmpty(applicationParameters.Authority) ? applicationParameters.Authority : null;
            CallbackPath = !string.IsNullOrEmpty(applicationParameters.CallbackPath) ? applicationParameters.CallbackPath : null;
            Scopes = !string.IsNullOrEmpty(applicationParameters.CalledApiScopes) ? applicationParameters.CalledApiScopes : null;
        }

        /// <summary>
        /// Updates AzureAdBlock object from existing appSettings.json
        /// </summary>
        /// <param name="azureAdToken"></param>
        public AzureAdBlock UpdateFromJToken(JToken azureAdToken)
        {
            JObject azureAdObj = JObject.FromObject(azureAdToken);

            ClientId ??= azureAdObj.GetValue(PropertyNames.ClientId)?.ToString(); // here, if the applicationparameters value is null, we use the existing app settings value
            Instance ??= azureAdObj.GetValue(PropertyNames.Instance)?.ToString();
            Domain ??= azureAdObj.GetValue(PropertyNames.Domain)?.ToString();
            TenantId ??= azureAdObj.GetValue(PropertyNames.TenantId)?.ToString();
            Authority ??= azureAdObj.GetValue(PropertyNames.Authority)?.ToString();
            CallbackPath ??= azureAdObj.GetValue(PropertyNames.CallbackPath)?.ToString();
            Scopes ??= azureAdObj.GetValue(PropertyNames.Scopes)?.ToString();
            ClientSecret ??= azureAdObj.GetValue(PropertyNames.ClientSecret)?.ToString();
            ClientCertificates ??= azureAdObj.GetValue(PropertyNames.ClientCertificates)?.ToObject<string[]>();

            return this;
        }

        public dynamic BlazorSettings => new
        {
            ClientId = ClientId ?? DefaultProperties.ClientId, // here, if a value is null, we could use the default properties
            Authority = Authority ?? (string.IsNullOrEmpty(Instance) || string.IsNullOrEmpty(TenantId) ? DefaultProperties.Authority : $"{Instance}{TenantId}"),
            ValidateAuthority = true
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

        public JObject ToJObject()
        {
            if (IsBlazorWasm)
            {
                return JObject.FromObject(BlazorSettings);
            }
            if (IsWebApi)
            {
                return JObject.FromObject(WebApiSettings);
            }

            return JObject.FromObject(WebAppSettings);
        }
    }

    public class ApiSettingsBlock
    {
        public string? BaseUrl;
        public string? Scopes;
    }
}
