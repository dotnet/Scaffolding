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
        public IGraphObjectRetriever GraphObjectRetriever { get; set; }
        private MsalTokenCredential TokenCredential { get; set; }

        public MsAADTool(string commandName, ProvisioningToolOptions provisioningToolOptions)
        {
            ProvisioningToolOptions = provisioningToolOptions;
            CommandName = commandName;
            ConsoleLogger = new ConsoleLogger(ProvisioningToolOptions.Json);
            TokenCredential = new MsalTokenCredential(ProvisioningToolOptions.TenantId, ProvisioningToolOptions.Username, ConsoleLogger);
            GraphServiceClient = new GraphServiceClient(new TokenCredentialAuthenticationProvider(TokenCredential));
            AzureManagementAPI = new AzureManagementAuthenticationProvider(TokenCredential);
            GraphObjectRetriever = new GraphObjectRetriever(GraphServiceClient, ConsoleLogger);
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

        internal async Task<string> PrintApplicationsList()
        {
            string outputJsonString = string.Empty;
            var applications = await GetApplicationsAsync();
            if (ProvisioningToolOptions.Json)
            {
                outputJsonString = new JsonResponse(CommandName, State.Success, applications).ToJsonString();
            }
            else
            {
                Console.Write(
                    "--------------------------------------------------------------\n" +
                    "Application Name\t\t\t\tApplication ID\n" +
                    "--------------------------------------------------------------\n\n");
                foreach (var app in applications)
                {
                    Console.WriteLine($"{app.DisplayName,-35}\t\t{app.AppId}");
                }
            }

            return outputJsonString;
        }

        internal async Task<IList<Application>> GetApplicationsAsync()
        {
            var graphObjectsList = await GraphObjectRetriever.GetGraphObjects();
            if (graphObjectsList is null)
            {
                ConsoleLogger.LogFailure(Resources.FailedToRetrieveADObjectsError, CommandName);
                Environment.Exit(1);
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
                Organization? tenant = await GraphObjectRetriever.GetTenant();
                if (tenant != null && tenant.TenantType.Equals("AAD B2C", StringComparison.OrdinalIgnoreCase))
                {
                    foreach (var app in applicationList)
                    {
                        app.AdditionalData.Add("IsB2C", true);
                    }
                }

                //order list by created date.
                applicationList = applicationList.OrderByDescending(app => app.CreatedDateTime).ToList();
            }

            return applicationList;
        }

        internal async Task<string> PrintServicePrincipalList()
        {
            string outputJsonString = string.Empty;
            var graphObjectsList = await GraphObjectRetriever.GetGraphObjects();
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
                            Console.WriteLine($"{sp.DisplayName,-35}\t\t{sp.AppId}");
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
                    using JsonDocument document = JsonDocument.Parse(tenantsJsonString);
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
                    Console.WriteLine($"{tenant.DisplayName ?? string.Empty,-16}\t\t{tenant.DefaultDomain ?? string.Empty,-20}\t\t{tenant.TenantType ?? string.Empty,-10}\t{tenant.TenantId ?? string.Empty}");
                }
            }
            return outputJsonString;
        }
    }
}
