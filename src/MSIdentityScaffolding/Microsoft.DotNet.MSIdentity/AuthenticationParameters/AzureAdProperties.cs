using System.Collections.Generic;

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
        public const string ValidateAuthority = "true";

        public const string MicrosoftGraphBaseUrl = "https://graph.microsoft.com/v1.0";
        public const string MicrosoftGraphScopes = "user.read";

        public static Dictionary<string, string> AzureAd => new Dictionary<string, string>
        {
            { PropertyNames.Domain, Domain },
            { PropertyNames.TenantId, TenantId },
            { PropertyNames.ClientId, ClientId },
            { PropertyNames.Instance, Instance },
            { PropertyNames.CallbackPath, CallbackPath },
        };

        public static Dictionary<string, string> AzureAdBlazor => new Dictionary<string, string>
        {
            { PropertyNames.Authority, Authority },
            { PropertyNames.ClientId, ClientId },
            { PropertyNames.ValidateAuthority, ValidateAuthority }
        };

        public static Dictionary<string, string> AllProperties => new Dictionary<string, string> // TODO this can be better
        {
            { PropertyNames.Domain, Domain },
            { PropertyNames.TenantId, TenantId },
            { PropertyNames.ClientId, ClientId },
            { PropertyNames.Instance, Instance },
            { PropertyNames.CallbackPath, CallbackPath },
            { PropertyNames.Authority, Authority },
            { PropertyNames.ValidateAuthority, ValidateAuthority }
        };

        public static Dictionary<string, string> MicrosoftGraphDefaults => new Dictionary<string, string>
        {
            { PropertyNames.BaseUrl, MicrosoftGraphBaseUrl },
            { PropertyNames.Scopes, MicrosoftGraphScopes }
        };

        public static Dictionary<string, string> DownstreamApiDefaults => new Dictionary<string, string>
        {
            { PropertyNames.BaseUrl, PropertyNames.BaseUrl }, // TODO
            { PropertyNames.Scopes, PropertyNames.BaseUrl } // TODO
        };
    }
}
