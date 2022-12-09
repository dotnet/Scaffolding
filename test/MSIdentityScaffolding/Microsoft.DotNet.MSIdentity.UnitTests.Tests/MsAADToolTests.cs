using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.DotNet.MSIdentity.DeveloperCredentials;
using Microsoft.DotNet.MSIdentity.MicrosoftIdentityPlatformApplication;
using Microsoft.DotNet.MSIdentity.Shared;
using Microsoft.DotNet.MSIdentity.Tool;
using Microsoft.Graph;
using Moq;
using Xunit;

namespace Microsoft.DotNet.MSIdentity.UnitTests.Tests
{
    public class MsAADToolTests
    {
        string tenantsJson =
            "{\"value\":[" +
                "{  \"id\":\"/tenants/AAAAAA-bbbbbb-CCCCCC-dddd-EEEEEEEE\",\"tenantId\":\"AAAAAA-bbbbbb-CCCCCC-dddd-EEEEEEEE\",\"countryCode\":\"US\",\"displayName\":\"NET AAD App\",\"domains\":[\"netaadapp.onmicrosoft.com\"],\"tenantCategory\":\"Home\",\"defaultDomain\":\"netaadapp.onmicrosoft.com\",\"tenantType\":\"AAD\"}," +
                "{  \"id\":\"/tenants/EEEEEE-dddddd-CCCCCC-bbbb-AAAAAAAA\",\"tenantId\":\"EEEEEE-dddddd-CCCCCC-bbbb-AAAAAAAA\",\"countryCode\":\"US\",\"displayName\":\"NET AAD B2C App\",\"domains\":[\"netaadb2capp.onmicrosoft.com\"],\"tenantCategory\":\"Home\",\"defaultDomain\":\"netaadb2capp.onmicrosoft.com\",\"tenantType\":\"AAD B2C\"}]}";

        public static ProvisioningToolOptions ToolOptions 
        {
            get 
            {
                return new ProvisioningToolOptions 
                {
                    TenantId = "abcdefg",
                    Username = "testUser",
                    Json = true
                };
            }
        }

        [Fact]
        public async void TestPrintTenantsList()
        {
            string path = Assembly.GetExecutingAssembly().Location;
            MsAADTool jsonTenantTool = new MsAADTool(Commands.LIST_TENANTS_COMMAND, ToolOptions);
            Mock<IAzureManagementAuthenticationProvider> azureMngProviderMock = new Mock<IAzureManagementAuthenticationProvider>();
            azureMngProviderMock.Setup(_ => _.ListTenantsAsync()).Returns(Task.FromResult(tenantsJson));
            jsonTenantTool.AzureManagementAPI = azureMngProviderMock.Object;
            string tenantsJsonFormatted = await jsonTenantTool.PrintTenantsList();
            if (string.IsNullOrEmpty(tenantsJsonFormatted))
            {
                Assert.True(false, "Formatting tenants from Azure Management failed");
            }
            var jsonResponse = JsonSerializer.Deserialize<JsonResponse>(tenantsJsonFormatted);
            var tenantJsonList = JsonSerializer.Deserialize<TenantInformation[]>(jsonResponse.Content.ToString());
            Assert.True(tenantJsonList.Any());
            Assert.True(tenantJsonList.Length == 2);
            var aadApp = tenantJsonList.Where(x => x.DisplayName.Equals("NET AAD App")).FirstOrDefault();
            var aadB2CApp = tenantJsonList.Where(x => x.DisplayName.Equals("NET AAD B2C App")).FirstOrDefault();
            Assert.True(aadApp != null && aadB2CApp != null);

            Assert.True(aadApp.TenantType.Equals("AAD"));
            Assert.True(!string.IsNullOrEmpty(aadApp.TenantId));

            Assert.True(aadB2CApp.TenantType.Equals("AAD B2C"));
            Assert.True(!string.IsNullOrEmpty(aadB2CApp.TenantId));
        }

        [Fact]
        public async void TestPrintApplicationsList_NoGraphObjects()
        {
            var directoryObjects = new List<DirectoryObject>();

            Mock<IGraphObjectRetriever> graphObjectRetriever = new Mock<IGraphObjectRetriever>();
            graphObjectRetriever.Setup(g => g.GetGraphObjects()).Returns(Task.FromResult(directoryObjects));

            MsAADTool jsonAppTool = new MsAADTool(Commands.LIST_AAD_APPS_COMMAND, ToolOptions)
            {
                GraphObjectRetriever = graphObjectRetriever.Object
            };

            var expected = "{\"Command\":\"--list-aad-apps\",\"State\":\"Success\",\"Content\":[],\"Output\":null}";

            var appsResponse = await jsonAppTool.PrintApplicationsList();
            Assert.Equal(expected, appsResponse);
        }

        [Fact]
        public async void TestGetApplications_OneAppB2C()
        {
            var app = new Application { AdditionalData = new Dictionary<string, object>() };
            var directoryObjects = new List<DirectoryObject> { app };
            var tenant = new Organization { TenantType = "AAD B2C" };
            Mock<IGraphObjectRetriever> graphObjectRetriever = new Mock<IGraphObjectRetriever>();
            graphObjectRetriever.Setup(g => g.GetGraphObjects()).Returns(Task.FromResult(directoryObjects));
            graphObjectRetriever.Setup(g => g.GetTenant()).Returns(Task.FromResult(tenant));

            MsAADTool jsonAppTool = new MsAADTool(Commands.LIST_AAD_APPS_COMMAND, ToolOptions)
            {
                GraphObjectRetriever = graphObjectRetriever.Object
            };

            var apps = await jsonAppTool.GetApplicationsAsync();
            Assert.Equal(1, apps.Count);
            var additionalData = apps.First()?.AdditionalData;
            Assert.NotNull(additionalData);
            Assert.True(additionalData.ContainsKey("IsB2C"));
            additionalData.TryGetValue("IsB2C", out var isB2C);
            Assert.True((bool)isB2C);
        }
    }
}
