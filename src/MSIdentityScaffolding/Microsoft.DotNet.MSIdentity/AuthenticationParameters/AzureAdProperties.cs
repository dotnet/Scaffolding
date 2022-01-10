namespace Microsoft.DotNet.MSIdentity.AuthenticationParameters
{
    public class AppSettings
    {
        public AzureAdProperties? AzureAd { get; set; }
        public ApiProperties? DownstreamApi { get; set; }
        public ApiProperties? MicrosoftGraph { get; set; }
    }

    public class AzureAdProperties
    {
        public string? Domain { get; set; }
        public string? TenantId { get; set; }
        public string? ClientId { get; set; }
        public string? ClientSecret { get; set; }
        public string? CallbackPath { get; set; }
        public string? Instance { get; set; }
    }

    public class ApiProperties
    {
        public string? BaseUrl { get; set; }
        public string? Scopes { get; set; }
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
}
