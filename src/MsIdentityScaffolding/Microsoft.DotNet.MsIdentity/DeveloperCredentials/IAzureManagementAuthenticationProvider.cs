using System.Threading.Tasks;

namespace Microsoft.DotNet.MsIdentity.DeveloperCredentials
{
    public interface IAzureManagementAuthenticationProvider
    {
        Task<string> ListTenantsAsync();
    }
}
