using System.Threading.Tasks;

namespace Microsoft.DotNet.MSIdentity.DeveloperCredentials
{
    public interface IAzureManagementAuthenticationProvider
    {
        Task<string> ListTenantsAsync();
    }
}
