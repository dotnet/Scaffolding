using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.DotNet.MsIdentity.AuthenticationParameters
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

    public static class AzureAdDefaultProperties
    {
        public const string Domain = "qualified.domain.name";
        public const string ClientId = "22222222-2222-2222-2222-222222222222";
        public const string TenantId = "11111111-1111-1111-11111111111111111";
        public const string Instance = "https://login.microsoftonline.com/";
        public const string CallbackPath = "/signin-oidc";
    }
}
