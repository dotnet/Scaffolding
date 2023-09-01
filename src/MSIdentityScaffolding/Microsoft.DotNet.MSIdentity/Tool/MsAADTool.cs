// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.DotNet.MSIdentity.AuthenticationParameters;
using Microsoft.DotNet.MSIdentity.DeveloperCredentials;
using Microsoft.DotNet.MSIdentity.MicrosoftIdentityPlatform;
using Microsoft.DotNet.MSIdentity.Shared;
using Microsoft.Graph;
using Microsoft.Graph.Models;

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
            ConsoleLogger = new ConsoleLogger(CommandName, ProvisioningToolOptions.Json);
            TokenCredential = new MsalTokenCredential(ProvisioningToolOptions.TenantId, ProvisioningToolOptions.Username, ProvisioningToolOptions.Instance, ConsoleLogger);
            GraphServiceClient = ProvisioningToolOptions.IsGovernmentCloud
                ? new GraphServiceClient(new TokenCredentialAuthenticationProvider(TokenCredential, new string[] { "https://graph.microsoft.us/.default" }), "https://graph.microsoft.us/v1.0")
                : new GraphServiceClient(new TokenCredentialAuthenticationProvider(TokenCredential));

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
            IList<Application> applicationList = new List<Application>();

            var graphObjectsList = await GraphObjectRetriever.GetGraphObjects(); // Will exit early if call fails
            foreach (var graphObj in graphObjectsList!)
            {
                if (graphObj is Application app)
                {
                    applicationList.Add(app);
                }
            }

            if (applicationList.Any())
            {
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
            IList<TenantInfo> tenantList = new List<TenantInfo>();
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
                                    new TenantInfo()
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
