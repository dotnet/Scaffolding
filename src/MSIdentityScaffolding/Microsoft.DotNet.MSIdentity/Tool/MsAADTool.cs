// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.DotNet.MSIdentity.AuthenticationParameters;
using Microsoft.DotNet.MSIdentity.DeveloperCredentials;
using Microsoft.DotNet.MSIdentity.MicrosoftIdentityPlatformApplication;
using Microsoft.DotNet.MSIdentity.Properties;
using Microsoft.DotNet.MSIdentity.Shared;
using Microsoft.Graph;

namespace Microsoft.DotNet.MSIdentity.Tool
{
    internal class MsAADTool : IMsAADTool
    {
        internal IConsoleLogger ConsoleLogger { get; }
        private ProvisioningToolOptions ProvisioningToolOptions { get; set; }
        private string CommandName { get; }
        public GraphServiceClient GraphServiceClient { get; set; }
        public IAzureManagementAuthenticationProvider AzureManagementAPI { get; set; }
        private MsalTokenCredential TokenCredential { get; set; }

        public MsAADTool(string commandName, ProvisioningToolOptions provisioningToolOptions)
        {
            ProvisioningToolOptions = provisioningToolOptions;
            CommandName = commandName;
            TokenCredential = new MsalTokenCredential(ProvisioningToolOptions.TenantId, ProvisioningToolOptions.Username);
            GraphServiceClient = new GraphServiceClient(new TokenCredentialAuthenticationProvider(TokenCredential));
            AzureManagementAPI = new AzureManagementAuthenticationProvider(TokenCredential);
            ConsoleLogger = new ConsoleLogger(ProvisioningToolOptions.Json);
        }

        public async Task<ApplicationParameters?> Run()
        {
            string outputJsonString = string.Empty;
            if (TokenCredential != null && GraphServiceClient != null)
            {
                switch (CommandName)
                {
                    //--list-aad-apps
                    case Commands.LIST_AAD_APPS_COMMAND:
                        outputJsonString = await PrintApplicationsList();
                        break;
                    //--list-service-principals
                    case Commands.LIST_SERVICE_PRINCIPALS_COMMAND:
                        outputJsonString = await PrintServicePrincipalList();
                        break;
                    //list-tenants
                    case Commands.LIST_TENANTS_COMMAND:
                        outputJsonString = await PrintTenantsList();
                        break;
                    default:
                        break;
                }
                if (ProvisioningToolOptions.Json)
                {
                    Console.WriteLine(outputJsonString);
                }
            }
            return null;
        }

        private async Task<List<DirectoryObject>?> GetGraphObjects()
        {
            List<DirectoryObject> graphObjectsList = new List<DirectoryObject>();
            try
            {
                var graphObjects = await GraphServiceClient.Me.OwnedObjects
                    .Request()
                    .GetAsync();

                if (graphObjects != null)
                {
                    graphObjectsList.AddRange(graphObjects.ToList());

                    var nextPage = graphObjects.NextPageRequest;
                    while (nextPage != null)
                    {
                        try
                        {
                            var additionalGraphObjects = await nextPage.GetAsync();
                            if (additionalGraphObjects != null)
                            {
                                graphObjectsList.AddRange(additionalGraphObjects.ToList());
                                nextPage = additionalGraphObjects.NextPageRequest;
                            }
                            else
                            {
                                nextPage = null;
                            }
                        }
                        catch (ServiceException)
                        {
                            nextPage = null;
                            ConsoleLogger.LogMessage(Resources.FailedToRetrieveADObjectsError, LogMessageType.Error);
                            return null;
                        }
                    }
                }
            }
            catch (ServiceException)
            {
                ConsoleLogger.LogMessage(Resources.FailedToRetrieveADObjectsError, LogMessageType.Error);
                return null;
            }

            return graphObjectsList;
        }

        internal async Task<string> PrintApplicationsList()
        {
            string outputJsonString = string.Empty;
            var graphObjectsList = await GetGraphObjects();
            if (graphObjectsList is null)
            {
                return new JsonResponse(CommandName, State.Fail, output: Resources.FailedToRetrieveADObjectsError).ToJsonString();
            }

            IList<Application> applicationList = new List<Application>();
            foreach (var graphObj in graphObjectsList)
            {
                if (graphObj is Application app)
                {
                    applicationList.Add(app);
                }
            }

            if (applicationList.Any())
            {
                Organization? tenant = await GetTenant(GraphServiceClient);
                if (tenant != null && tenant.TenantType.Equals("AAD B2C", StringComparison.OrdinalIgnoreCase))
                {
                    foreach (Application app in applicationList)
                    {
                        app.AdditionalData.Add("IsB2C", true);
                    }
                }

                //order list by created date.
                applicationList = applicationList.OrderByDescending(app => app.CreatedDateTime).ToList();
            }

            if (ProvisioningToolOptions.Json)
            {
                outputJsonString = new JsonResponse(CommandName, State.Success, applicationList).ToJsonString();
            }
            else
            {
                Console.Write(
                    "--------------------------------------------------------------\n" +
                    "Application Name\t\t\t\tApplication ID\n" +
                    "--------------------------------------------------------------\n\n");
                foreach (var app in applicationList)
                {
                    Console.WriteLine($"{app.DisplayName.PadRight(35)}\t\t{app.AppId}");
                }
            }

            return outputJsonString;
        }

        private static async Task<Organization?> GetTenant(GraphServiceClient graphServiceClient)
        {
            Organization? tenant;
            try
            {
                tenant = (await graphServiceClient.Organization
                    .Request()
                    .GetAsync()).FirstOrDefault();
            }
            catch (ServiceException ex)
            {
                string? errorMessage;
                if (ex.InnerException != null)
                {
                    errorMessage = ex.InnerException.Message;
                }
                else
                {
                    if (ex.Message.Contains("User was not found") || ex.Message.Contains("not found in tenant"))
                    {
                        errorMessage = "User was not found.\nUse both --tenant-id <tenant> --username <username@tenant>.\nAnd re-run the tool.";
                    }
                    else
                    {
                        errorMessage = ex.Message;
                    }
                }

                Console.WriteLine(errorMessage);
                return null;
            }

            return tenant;
        }


        internal async Task<string> PrintServicePrincipalList()
        {
            string outputJsonString = string.Empty;
            var graphObjectsList = await GetGraphObjects();
            IList<ServicePrincipal> servicePrincipalList = new List<ServicePrincipal>();

            if (graphObjectsList != null && graphObjectsList.Any())
            {
                foreach (var graphObj in graphObjectsList)
                {
                    if (graphObj is ServicePrincipal servicePrincipal)
                    {
                        servicePrincipalList.Add(servicePrincipal);
                    }
                }
                if (servicePrincipalList.Any())
                {
                    if (ProvisioningToolOptions.Json)
                    {
                        JsonResponse jsonResponse = new JsonResponse(CommandName, State.Success, servicePrincipalList);
                        outputJsonString = jsonResponse.ToJsonString();
                    }
                    else
                    {
                        Console.Write(
                            "--------------------------------------------------------------\n" +
                            "Application Name\t\t\t\tApplication ID\n" +
                            "--------------------------------------------------------------\n\n");
                        foreach (var sp in servicePrincipalList)
                        {
                            Console.WriteLine($"{sp.DisplayName.PadRight(35)}\t\t{sp.AppId}");
                        }
                    }
                }
            }
            return outputJsonString;
        }

        internal async Task<string> PrintTenantsList()
        {
            string outputJsonString = string.Empty;
            IList<TenantInformation> tenantList = new List<TenantInformation>();
            if (AzureManagementAPI != null)
            {
                var tenantsJsonString = await AzureManagementAPI.ListTenantsAsync();
                if (!string.IsNullOrEmpty(tenantsJsonString))
                {
                    using (JsonDocument document = JsonDocument.Parse(tenantsJsonString))
                    {
                        if (document.RootElement.TryGetProperty("value", out JsonElement jsonTenantElement))
                        {
                            var jsonTenantEnumerator = jsonTenantElement.EnumerateArray();
                            if (jsonTenantEnumerator.Any())
                            {
                                while (jsonTenantEnumerator.MoveNext())
                                {
                                    JsonElement current = jsonTenantEnumerator.Current;
                                    string? tenantId = current.GetProperty("tenantId").GetString();
                                    string? tenantType = current.GetProperty("tenantType").GetString();
                                    string? defaultDomain = current.GetProperty("defaultDomain").GetString();
                                    string? displayName = current.GetProperty("displayName").GetString();

                                    tenantList.Add(
                                        new TenantInformation()
                                        {
                                            TenantId = tenantId,
                                            TenantType = tenantType,
                                            DefaultDomain = defaultDomain,
                                            DisplayName = displayName
                                        });
                                }
                            }
                        }
                    }
                }
            }

            if (ProvisioningToolOptions.Json)
            {
                JsonResponse jsonResponse = new JsonResponse(CommandName, State.Success, tenantList);
                outputJsonString = jsonResponse.ToJsonString();
            }
            else
            {
                Console.Write(
                    "--------------------------------------------------------------------------------------------------------------------------------\n" +
                    "Display Name\t\t\tDefault Domain\t\t\t\tTenant Type\tTenant Id\n" +
                    "--------------------------------------------------------------------------------------------------------------------------------\n\n");
                foreach (var tenant in tenantList)
                {
                    Console.WriteLine($"{(tenant.DisplayName ?? string.Empty).PadRight(16)}\t\t{(tenant.DefaultDomain ?? string.Empty).PadRight(20)}\t\t{(tenant.TenantType ?? string.Empty).PadRight(10)}\t{(tenant.TenantId ?? string.Empty)}");
                }
            }
            return outputJsonString;
        }
    }
}
