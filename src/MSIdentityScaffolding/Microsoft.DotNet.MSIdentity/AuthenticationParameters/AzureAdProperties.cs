using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.DotNet.MSIdentity.AuthenticationParameters
{
    public class AzureAdProperties
    {
        public string? Domain { get; set; }
        public string? TenantId { get; set; }
        public string? ClientId { get; set; }
        public string? ClientSecret { get; set; }
        public string? CallbackPath { get; set; }
        public string? Instance { get; set; }
    }

    public class BlazorProperties
    {
        public string? Authority { get; set; }
        public string? ClientId { get; set; }
        public string? ValidateAuthority { get; set; }
    }

    //getting default properties from https://github.com/dotnet/aspnetcore/blob/6bc4b79f4ee7af00edcbb435e5ee4c1de349a110/src/ProjectTemplates/Web.ProjectTemplates/content/StarterWeb-CSharp/appsettings.json
    public static class AzureAdDefaultProperties
    {
        public const string Domain = "qualified.domain.name";
        public const string TenantId = "22222222-2222-2222-2222-222222222222";
        public const string ClientId = "11111111-1111-1111-11111111111111111";
        public const string Instance = "https://login.microsoftonline.com/";
        public const string CallbackPath = "/signin-oidc";
    }

    public static class BlazorDefaultProperties
    {
        public const string Authority = AzureAdDefaultProperties.Instance + AzureAdDefaultProperties.TenantId;
        public const string ClientId = AzureAdDefaultProperties.ClientId;
        public const string ValidateAuthority = "true";
    }
}
