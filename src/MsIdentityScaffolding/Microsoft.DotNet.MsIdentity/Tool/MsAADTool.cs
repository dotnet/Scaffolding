// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Graph;
using Microsoft.DotNet.MsIdentity;
using Microsoft.DotNet.MsIdentity.AuthenticationParameters;
using Microsoft.DotNet.MsIdentity.DeveloperCredentials;
using Microsoft.DotNet.MsIdentity.MicrosoftIdentityPlatformApplication;

namespace Microsoft.DotNet.MsIdentity
{
    public class MsAADTool : IMsAADTool
    {
        private ProvisioningToolOptions ProvisioningToolOptions { get; set; }
        private string _commandName { get; set; }
        private GraphServiceClient _graphServiceClient;
        private MsalTokenCredential _tokenCredential;

        public MsAADTool(string commandName, ProvisioningToolOptions provisioningToolOptions)
        {
            ProvisioningToolOptions = provisioningToolOptions;
            _commandName = commandName;
            _tokenCredential = new MsalTokenCredential(provisioningToolOptions.TenantId, provisioningToolOptions.Username);
            _graphServiceClient = new GraphServiceClient(new TokenCredentialAuthenticationProvider(_tokenCredential));
        }

        public async Task<ApplicationParameters?> Run()
        {
            if (_tokenCredential != null && _graphServiceClient != null)
            {
                var graphObjects = await _graphServiceClient.Me.OwnedObjects
                                            .Request()
                                            .GetAsync();

                if (graphObjects.Any())
                {
                    switch(_commandName)
                    {
                        //--list-aad-apps
                        case Commands.LIST_AAD_APPS_COMMAND:
                            PrintApplicationsList(graphObjects.ToList(), ProvisioningToolOptions.Json ?? false);
                            break;
                        //--list-service-principals
                        case Commands.LIST_SERVICE_PRINCIPALS_COMMAND:
                            PrintServicePrincipalList(graphObjects.ToList(), ProvisioningToolOptions.Json ?? false);
                            break;
                        default:
                            break;
                    }
                }

            }
            return null;
        }

        private void PrintApplicationsList(IList<DirectoryObject> graphObjects, bool outputJson)
        {
            IList<Application> applicationList = new List<Application>();
            if (graphObjects != null)
            {
                foreach (var graphObj in graphObjects)
                {
                    if (graphObj is Application app)
                    {
                        {
                            applicationList.Add(app);
                        }
                    }
                }

                if (applicationList.Any())
                { 
                    if (outputJson)
                    {
                        string outputString = JsonSerializer.Serialize(applicationList);
                        Console.WriteLine(outputString);
                    }
                    else 
                    {
                        Console.Write(
                            "--------------------------------------------------------------\n" + 
                            "Application Name\t\t\t\tApplication ID\n" +
                            "--------------------------------------------------------------\n\n");
                        foreach(var app in applicationList)
                        {
                           Console.WriteLine($"{app.DisplayName.PadRight(35)}\t\t{app.AppId}");
                        }
                    }
                }
            }
        }

        private void PrintServicePrincipalList(IList<DirectoryObject> graphObjects, bool outputJson)
        {
            IList<ServicePrincipal> servicePrincipalList = new List<ServicePrincipal>();
            if (graphObjects != null)
            {
                foreach (var graphObj in graphObjects)
                {
                    if (graphObj is ServicePrincipal servicePrincipal)
                    {
                        servicePrincipalList.Add(servicePrincipal);          
                    }
                }

                string outputString = string.Empty;
                if (servicePrincipalList.Any())
                { 
                    if (outputJson)
                    {
                        outputString = JsonSerializer.Serialize(servicePrincipalList);
                    }
                    else 
                    {
                        Console.Write(
                            "--------------------------------------------------------------\n" + 
                            "Application Name\t\t\t\tApplication ID\n" +
                            "--------------------------------------------------------------\n\n");
                        foreach(var app in servicePrincipalList)
                        {
                           Console.WriteLine($"{app.DisplayName.PadRight(35)}\t\t{app.AppId}");
                        }
                    }
                    //TODO do we need an else scenario where we list Service Principals for the command line experience.
                }
                Console.WriteLine(outputString);
            }
        }
    }
}
