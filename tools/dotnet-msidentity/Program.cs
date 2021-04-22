// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
using System.Collections.Generic;
using System.CommandLine;
using System.CommandLine.Builder;
using System.CommandLine.Invocation;
using System.CommandLine.Parsing;
using System.Diagnostics;
using System.Threading.Tasks;

namespace Microsoft.DotNet.MSIdentity.Tool
{
    public static class Program
    {
        public static async Task<int> Main(string []args)
        {
            var rootCommand = MsIdentityCommand();
            var listAadAppsCommand = ListAADAppsCommand();
            var listServicePrincipalsCommand = ListServicePrincipalsCommand();
            var listTenantsCommand = ListTenantsCommand();
            var registerApplicationCommand = RegisterApplicationCommand();
            var unregisterApplicationCommand = UnregisterApplicationCommand();
            var updateApplicationCommand = UpdateApplicationCommand();
            var updateProjectCommand = UpdateProjectCommand();
            var addClientSecretCommand = AddClientSecretCommand();
            //hide internal commands.
            listAadAppsCommand.IsHidden = true;
            listServicePrincipalsCommand.IsHidden = true;
            listTenantsCommand.IsHidden = true;
            updateProjectCommand.IsHidden = true;
            addClientSecretCommand.IsHidden = true;

            listAadAppsCommand.Handler = CommandHandler.Create<ProvisioningToolOptions>(HandleListApps);
            listServicePrincipalsCommand.Handler = CommandHandler.Create<ProvisioningToolOptions>(HandleListServicePrincipals);
            listTenantsCommand.Handler = CommandHandler.Create<ProvisioningToolOptions>(HandleListTenants);
            registerApplicationCommand.Handler = CommandHandler.Create<ProvisioningToolOptions>(HandleRegisterApplication);
            unregisterApplicationCommand.Handler = CommandHandler.Create<ProvisioningToolOptions>(HandleUnregisterApplication);
            updateApplicationCommand.Handler = CommandHandler.Create<ProvisioningToolOptions>(HandleUpdateApplication);
            updateProjectCommand.Handler = CommandHandler.Create<ProvisioningToolOptions>(HandleUpdateProject);
            addClientSecretCommand.Handler = CommandHandler.Create<ProvisioningToolOptions>(HandleClientSecrets);

            //add all commands to root command.
            rootCommand.AddCommand(listAadAppsCommand);
            rootCommand.AddCommand(listServicePrincipalsCommand);
            rootCommand.AddCommand(listTenantsCommand);
            rootCommand.AddCommand(registerApplicationCommand);
            rootCommand.AddCommand(unregisterApplicationCommand);
            rootCommand.AddCommand(updateApplicationCommand);
            rootCommand.AddCommand(updateProjectCommand);
            rootCommand.AddCommand(addClientSecretCommand);

            //if no args are present, show default help.
            if (args == null || args.Length == 0)
            {
                args = new string[] { "-h" };
            }

            var commandLineBuilder = new CommandLineBuilder(rootCommand);
            commandLineBuilder.UseDefaults();

            var parser = commandLineBuilder.Build();
            return await parser.InvokeAsync(args);
        }

        private static async Task<int> HandleListApps(ProvisioningToolOptions provisioningToolOptions)
        {
            if (provisioningToolOptions != null)
            {
                IMsAADTool msAADTool = MsAADToolFactory.CreateTool(Commands.LIST_AAD_APPS_COMMAND, provisioningToolOptions);
                await msAADTool.Run();
                return 0;
            }
            return -1;
        }

        private static async Task<int> HandleListTenants(ProvisioningToolOptions provisioningToolOptions)
        {
            if (provisioningToolOptions != null)
            {
                IMsAADTool msAADTool = MsAADToolFactory.CreateTool(Commands.LIST_TENANTS_COMMAND, provisioningToolOptions);
                await msAADTool.Run();
                return 0;
            }
            return -1;
        }

        private static async Task<int> HandleListServicePrincipals(ProvisioningToolOptions provisioningToolOptions)
        {
            if (provisioningToolOptions != null)
            {
                IMsAADTool msAADTool = MsAADToolFactory.CreateTool(Commands.LIST_SERVICE_PRINCIPALS_COMMAND, provisioningToolOptions);
                await msAADTool.Run();
                return 0;
            }
            return -1;
        }

        private static async Task<int> HandleRegisterApplication(ProvisioningToolOptions provisioningToolOptions)
        {
            if (provisioningToolOptions != null)
            {
                IMsAADTool msAADTool = MsAADToolFactory.CreateTool(Commands.REGISTER_APPLICATIION_COMMAND, provisioningToolOptions);
                await msAADTool.Run();
                return 0;
            }
            return -1;
        }

        private static async Task<int> HandleUpdateApplication(ProvisioningToolOptions provisioningToolOptions)
        {
            if (provisioningToolOptions != null)
            {
                IMsAADTool msAADTool = MsAADToolFactory.CreateTool(Commands.UPDATE_APPLICATION_COMMAND, provisioningToolOptions);
                await msAADTool.Run();
                return 0;
            }
            return -1;
        }

        private static async Task<int> HandleUnregisterApplication(ProvisioningToolOptions provisioningToolOptions)
        {
            if (provisioningToolOptions != null)
            {
                IMsAADTool msAADTool = MsAADToolFactory.CreateTool(Commands.UNREGISTER_APPLICATION_COMMAND, provisioningToolOptions);
                await msAADTool.Run();
                return 0;
            }
            return -1;
        }

        private static async Task<int> HandleUpdateProject(ProvisioningToolOptions provisioningToolOptions)
        {
            if (provisioningToolOptions != null)
            {
                IMsAADTool msAADTool = MsAADToolFactory.CreateTool(Commands.UPDATE_PROJECT_COMMAND, provisioningToolOptions);
                await msAADTool.Run();
                return 0;
            }
            return -1;
        }

        private static async Task<int> HandleClientSecrets(ProvisioningToolOptions provisioningToolOptions)
        {
            if (provisioningToolOptions != null)
            {
                IMsAADTool msAADTool = MsAADToolFactory.CreateTool(Commands.ADD_CLIENT_SECRET, provisioningToolOptions);
                await msAADTool.Run();
                return 0;
            }
            return -1;
        }
        
        private static RootCommand MsIdentityCommand()=>
            new RootCommand(
                description: "Creates or updates an Azure AD / Azure AD B2C application, and updates the code, using your developer credentials (from Visual Studio, Azure CLI, Azure RM PowerShell, VS Code).")
            {
            };

        private static Command ListAADAppsCommand()=>
            new Command(
                name: Commands.LIST_AAD_APPS_COMMAND,
                description: "Lists AAD Applications for a given tenant/username.")
            {
                TenantOption(), UsernameOption(), JsonOption()
            };

        private static Command ListServicePrincipalsCommand()=>
            new Command(
                name: Commands.LIST_SERVICE_PRINCIPALS_COMMAND,
                description: "Lists AAD Service Principals.")
            {
                TenantOption(), UsernameOption(), JsonOption()
            };

        private static Command ListTenantsCommand()=>
            new Command(
                name: Commands.LIST_TENANTS_COMMAND,
                description: "Lists AAD and AAD B2C tenants for a given user.")
            {
                UsernameOption(), JsonOption()
            };

        private static Command AddClientSecretCommand()=>
            new Command(
                name: Commands.ADD_CLIENT_SECRET,
                description: "Create client secret for an Azure AD/AD B2C application.")
            {
                TenantOption(), UsernameOption(), JsonOption(), ClientIdOption(), ProjectPathOption(), ProjectFilePathOption(), UpdateUserSecretsOption()
            };

        private static Command RegisterApplicationCommand()=>
            new Command(
                name: Commands.REGISTER_APPLICATIION_COMMAND,
                description: "Register an AAD/AAD B2C application in Azure and updates .NET application." + 
                             "\n\t- Updates the appsettings.json file.")
            {
                TenantOption(), UsernameOption(), JsonOption(), ClientIdOption(), ClientSecretOption(), AppIdUriOption(), ApiClientIdOption(), SusiPolicyIdOption(), ProjectPathOption(), ProjectFilePathOption()
            };

        private static Command UpdateProjectCommand()=>
            new Command(
                name: Commands.UPDATE_PROJECT_COMMAND,
                description: "Update an AAD/AAD B2C application in Azure and .NET application." + 
                             "\n\t- Updates the appsettings.json file." + 
                             "\n\t- Updates the Startup.cs file." + 
                             "\n\t- Updates the user secrets.")
            {
                TenantOption(), UsernameOption(), JsonOption(), ProjectPathOption(), ClientIdOption(), CallsGraphOption(), CallsDownstreamApiOption(), UpdateUserSecretsOption(), ProjectFilePathOption(), RedirectUriOption()
            };

        private static Command UpdateApplicationCommand() =>
            new Command(
                name: Commands.UPDATE_APPLICATION_COMMAND,
                description: "Update an AAD/AAD B2C application in Azure." +
                             "\n\t- Updates the appsettings.json file.")
            {
                TenantOption(), UsernameOption(), JsonOption(), AppIdUriOption(), ClientIdOption(), RedirectUriOption(), EnableIdTokenOption(), EnableAccessToken()
            };

        private static Command UnregisterApplicationCommand() =>
            new Command(
                name: Commands.UNREGISTER_APPLICATION_COMMAND,

                description: "Unregister an AAD/AAD B2C application in Azure." +
                             "\n\t- Updates the appsettings.json file.")
            {
                TenantOption(), UsernameOption(), JsonOption(), AppIdUriOption(), ProjectPathOption(), ClientIdOption()
            };

        private static Option JsonOption()=>
            new Option<bool>(
                aliases: new [] {"-j", "--json"},
                description: "Output format for commands.")
            {
                IsRequired = false
            };

        private static Option EnableIdTokenOption() =>
            new Option<bool>(
                aliases: new[] { "--enable-id-token" },
                description: "Enable id token.")
            {
                IsRequired = false
            };

        private static Option EnableAccessToken() =>
            new Option<bool>(
                aliases: new[] { "--enable-access-token" },
                description: "Enable access token")
            {
                IsRequired = false
            };

        private static Option CallsGraphOption()=>
            new Option<bool>(
                aliases: new [] {"--calls-graph"},
                description: "App registration calls microsoft graph.")
            {
                IsRequired = false
            };

        private static Option CallsDownstreamApiOption()=>
            new Option<bool>(
                aliases: new [] {"--calls-downstream-api"},
                description: "App registration calls downstream api.")
            {
                IsRequired = false
            };

        private static Option UpdateUserSecretsOption()=>
            new Option<bool>(
                aliases: new [] {"--update-user-secrets"},
                description: "Add secrets to user secrets.json file." + 
                             "\n\t- Using dotnet-user-secrets to init and set user secrets.")
            {
                IsRequired = false
            };

        private static Option ClientIdOption()=>
            new Option<string>(
                aliases: new [] {"--client-id"},
                description: "Client ID of an existing application from which to update the code." +
                             "\n\tThis is used when you don't want to register a new app, but want to configure the code from an existing application (which can also be updated by the tool if needed)." +
                             "\n\t- You might want to also pass-in the --client-secret if you know it.")
            {
                IsRequired = false
            };

        private static Option ClientSecretOption()=>
            new Option<string>(
                aliases: new [] {"--client-secret"},
                description: "Client secret to use as a client credential.")
            {
                IsRequired = false
            };

        private static Option RedirectUriOption() =>
            new Option<IList<string>>(
                aliases: new[] { "--redirect-uris" },
                description: "Add redirect URIs (web) for the app.")
            {
                IsRequired = false
            };

        private static Option ProjectPathOption()=>
            new Option<string>(
                aliases: new [] {"-p", "--project-path"},
                description: "When specified, will analyze the application code in the specified folder. Otherwise analyzes the code in the current directory..")
            {
                IsRequired = false
            };

        private static Option ProjectFilePathOption()=>
            new Option<string>(
                aliases: new [] {"--project-file-path"},
                description: "When specified, will analyze the application specified by the csproj file. Otherwise analyzes the csproj in the current directory..")
            {
                IsRequired = false
            };
        
        private static Option AppIdUriOption()=>
            new Option<string>(
                aliases: new [] {"--app-id-uri"},
                description: "The App ID Uri for the blazorwasm hosted API. It's only used on the case of a blazorwasm hosted application (named after the --app-id-uri  blazorwasm template parameter).")
            {
                IsRequired = false
            };

        private static Option ApiClientIdOption()=>
            new Option<string>(
                aliases: new [] {"--api-client-id"},
                description: "Client ID of the blazorwasm hosted web API." +
                             "\nThis is only used on the case of a blazorwasm hosted application where you only want to configure the code (named after the --api-client-id blazorwasm  template parameter).")
            {
                IsRequired = false
            };
        private static Option SusiPolicyIdOption()=>
            new Option<string>(
                aliases: new [] {"--susi-policy-id"},
                description: "Sign-up/Sign-in policy required for configurating a B2C application from code that was created for AAD.")
            {
                IsRequired = false
            };
        private static Option TenantOption()=>
            new Option<string>(
                aliases: new[] {"-t", "--tenant-id"},
                description: "Azure AD or Azure AD B2C tenant in which to create/update the app.\n - If specified, the tool will create the application in the specified tenant.\n - Otherwise it will create the app in your home tenant")
            {
                IsRequired = false
            };

        private static Option UsernameOption()=>
            new Option<string>(
                aliases: new[] {"-u", "--username"},
                description:"Username to use to connect to the Azure AD or Azure AD B2C tenant." + 
                            "\n- It's only needed when you are signed-in in Visual Studio, or Azure CLI with several identities." + 
                            "\n- In that case, the username param is used to disambiguate which  identity to use to create the app in the tenant.")
            {
                IsRequired = false   
            };
    }
}
