// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
using System.Collections.Generic;
using System.CommandLine;
using System.CommandLine.Builder;
using System.CommandLine.Invocation;
using System.CommandLine.Parsing;
using System.Threading.Tasks;

namespace Microsoft.DotNet.MSIdentity.Tool
{
    public static class Program
    {
        public static async Task<int> Main(string[] args)
        {
            var rootCommand = MsIdentityCommand();

            //internal commands
            var listAadAppsCommand = ListAADAppsCommand();
            var listServicePrincipalsCommand = ListServicePrincipalsCommand();
            var listTenantsCommand = ListTenantsCommand();
            var createClientSecretCommand = CreateClientSecretCommand();

            //exposed commands
            var registerApplicationCommand = RegisterApplicationCommand();
            var unregisterApplicationCommand = UnregisterApplicationCommand();
            var updateAppRegistrationCommand = UpdateAppRegistrationCommand();
            var updateProjectCommand = UpdateProjectCommand();
            var createAppRegistration = CreateAppRegistrationCommand();

            //hide internal commands.
            listAadAppsCommand.IsHidden = true;
            listServicePrincipalsCommand.IsHidden = true;
            listTenantsCommand.IsHidden = true;
            updateProjectCommand.IsHidden = true;
            createClientSecretCommand.IsHidden = true;

            listAadAppsCommand.Handler = CommandHandler.Create<ProvisioningToolOptions>(HandleListApps);
            listServicePrincipalsCommand.Handler = CommandHandler.Create<ProvisioningToolOptions>(HandleListServicePrincipals);
            listTenantsCommand.Handler = CommandHandler.Create<ProvisioningToolOptions>(HandleListTenants);
            registerApplicationCommand.Handler = CommandHandler.Create<ProvisioningToolOptions>(HandleRegisterApplication);
            unregisterApplicationCommand.Handler = CommandHandler.Create<ProvisioningToolOptions>(HandleUnregisterApplication);
            createAppRegistration.Handler = CommandHandler.Create<ProvisioningToolOptions>(HandleCreateAppRegistration);
            updateAppRegistrationCommand.Handler = CommandHandler.Create<ProvisioningToolOptions>(HandleUpdateApplication);
            updateProjectCommand.Handler = CommandHandler.Create<ProvisioningToolOptions>(HandleUpdateProject);
            createClientSecretCommand.Handler = CommandHandler.Create<ProvisioningToolOptions>(HandleClientSecrets);

            //add all commands to root command.
            rootCommand.AddCommand(listAadAppsCommand);
            rootCommand.AddCommand(listServicePrincipalsCommand);
            rootCommand.AddCommand(listTenantsCommand);
            rootCommand.AddCommand(registerApplicationCommand);
            rootCommand.AddCommand(unregisterApplicationCommand);
            rootCommand.AddCommand(updateAppRegistrationCommand);
            rootCommand.AddCommand(updateProjectCommand);
            rootCommand.AddCommand(createClientSecretCommand);
            rootCommand.AddCommand(createAppRegistration);

            //if no args are present, show default help.
            if (args == null || args.Length == 0)
            {
                args = new string[] { "-h" };
            }

            var commandLineBuilder = new CommandLineBuilder(rootCommand);
            commandLineBuilder.UseDefaults();

            var parser = commandLineBuilder.Build();
            System.Diagnostics.Debugger.Launch();
            return await parser.InvokeAsync(args);
        }

        private static async Task<int> HandleListApps(ProvisioningToolOptions provisioningToolOptions)
        {
            if (provisioningToolOptions != null)
            {
                Helper.AddPackage();
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
                Helper.AddPackage();
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
                Helper.AddPackage();
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
                Helper.AddPackage();
                await msAADTool.Run();
                return 0;
            }
            return -1;
        }

        private static async Task<int> HandleUpdateApplication(ProvisioningToolOptions provisioningToolOptions)
        {
            if (provisioningToolOptions != null)
            {
                IMsAADTool msAADTool = MsAADToolFactory.CreateTool(Commands.UPDATE_APP_REGISTRATION_COMMAND, provisioningToolOptions);
                Helper.AddPackage();
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
                Helper.AddPackage();
                await msAADTool.Run();
                return 0;
            }
            return -1;
        }

        private static async Task<int> HandleCreateAppRegistration(ProvisioningToolOptions provisioningToolOptions)
        {
            if (provisioningToolOptions != null)
            {
                IMsAADTool msAADTool = MsAADToolFactory.CreateTool(Commands.CREATE_APP_REGISTRATION_COMMAND, provisioningToolOptions);
                Helper.AddPackage();
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
                Helper.AddPackage();
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
                Helper.AddPackage();
                await msAADTool.Run();
                return 0;
            }
            return -1;
        }

        private static RootCommand MsIdentityCommand() =>
            new RootCommand(
                description: "Creates or updates an Azure AD / Azure AD B2C application, and updates the project, using your developer credentials (from Visual Studio, Azure CLI, Azure RM PowerShell, VS Code).\n")
            {
            };

        private static Command ListAADAppsCommand() =>
            new Command(
                name: Commands.LIST_AAD_APPS_COMMAND,
                description: "Lists AAD Applications for a given tenant/username.\n")
            {
                TenantOption(), UsernameOption(), JsonOption()
            };

        private static Command ListServicePrincipalsCommand() =>
            new Command(
                name: Commands.LIST_SERVICE_PRINCIPALS_COMMAND,
                description: "Lists AAD Service Principals.\n")
            {
                TenantOption(), UsernameOption(), JsonOption()
            };

        private static Command ListTenantsCommand() =>
            new Command(
                name: Commands.LIST_TENANTS_COMMAND,
                description: "Lists AAD and AAD B2C tenants for a given user.\n")
            {
                UsernameOption(), JsonOption()
            };

        private static Command CreateClientSecretCommand() =>
            new Command(
                name: Commands.ADD_CLIENT_SECRET,
                description: "Create client secret for an Azure AD or AD B2C app registration.\n")
            {
                TenantOption(), UsernameOption(), JsonOption(), ClientIdOption(), ProjectFilePathOption(), UpdateUserSecretsOption()
            };

        private static Command RegisterApplicationCommand() =>
            new Command(
                name: Commands.REGISTER_APPLICATIION_COMMAND,
                description: "Register an Azure AD or Azure AD B2C app registration in Azure and update the project." +
                             "\n\t- Updates the appsettings.json file.\n")
            {
                TenantOption(), UsernameOption(), JsonOption(), ClientIdOption(), ClientSecretOption(), AppIdUriOption(), ApiClientIdOption(), SusiPolicyIdOption(), ProjectFilePathOption()
            };

        private static Command UpdateProjectCommand() =>
            new Command(
                name: Commands.UPDATE_PROJECT_COMMAND,
                description: "Update an Azure AD/AD B2C app registration in Azure and the project." +
                             "\n\t- Updates the appsettings.json file." +
                             "\n\t- Updates the Startup.cs file." +
                             "\n\t- Updates the user secrets.\n")
            {
                TenantOption(), UsernameOption(), ClientIdOption(), JsonOption(), ProjectFilePathOption(), ConfigUpdateOption(), CodeUpdateOption(), PackagesUpdateOption(), CallsGraphOption(), CallsDownstreamApiOption(), UpdateUserSecretsOption(), RedirectUriOption(),
            };

        private static Command UpdateAppRegistrationCommand() =>
            new Command(
                name: Commands.UPDATE_APP_REGISTRATION_COMMAND,
                description: "Update an Azure AD/AD B2C app registration in Azure.\n")
            {
                TenantOption(), UsernameOption(), JsonOption(), AppIdUriOption(), ClientIdOption(), RedirectUriOption(), EnableIdTokenOption(), EnableAccessToken()
            };

        private static Command CreateAppRegistrationCommand() =>
            new Command(
                name: Commands.CREATE_APP_REGISTRATION_COMMAND,
                description: "Create an Azure AD/AD B2C app registration in Azure.\n")
            {
                TenantOption(), UsernameOption(), JsonOption(), AppDisplayName(), ProjectFilePathOption(), ProjectType()
            };

        private static Command UnregisterApplicationCommand() =>
            new Command(
                name: Commands.UNREGISTER_APPLICATION_COMMAND,

                description: "Unregister an Azure AD or Azure AD B2C app registration in Azure." +
                             "\n\t- Updates the appsettings.json file.\n")
            {
                TenantOption(), UsernameOption(), JsonOption(), AppIdUriOption(), ProjectFilePathOption(), ClientIdOption()
            };

        private static Option JsonOption() =>
            new Option<bool>(
                aliases: new[] { "-j", "--json" },
                description: "Format output in JSON instead of text.\n")
            {
                IsRequired = false
            };

        private static Option EnableIdTokenOption() =>
            new Option<bool>(
                aliases: new[] { "--enable-id-token" },
                description: "Enable id token.\n")
            {
                IsRequired = false
            };

        private static Option EnableAccessToken() =>
            new Option<bool>(
                aliases: new[] { "--enable-access-token" },
                description: "Enable access token.\n")
            {
                IsRequired = false
            };

        private static Option CallsGraphOption() =>
            new Option<bool>(
                aliases: new[] { "--calls-graph" },
                description: "App registration calls microsoft graph.\n")
            {
                IsRequired = false
            };

        private static Option CallsDownstreamApiOption() =>
            new Option<bool>(
                aliases: new[] { "--calls-downstream-api" },
                description: "App registration calls downstream api.\n")
            {
                IsRequired = false
            };

        private static Option UpdateUserSecretsOption() =>
            new Option<bool>(
                aliases: new[] { "--update-user-secrets" },
                description: "Add secrets to user secrets.json file." +
                             "\n\t- Using dotnet-user-secrets to init and set user secrets.\n")
            {
                IsRequired = false
            };

        private static Option ConfigUpdateOption() =>
            new Option<bool>(
                aliases: new[] { "--config-update" },
                description: "Allow config changes for dotnet app to work with Azure AD/AD B2C app.")
            {
                IsRequired = false
            };


        private static Option CodeUpdateOption() =>
            new Option<bool>(
                aliases: new[] { "--code-update" },
                description: "Allow Startup.cs and other code changes for dotnet app to work with Azure AD/AD B2C app (setup authentication).")
            {
                IsRequired = false
            };

        private static Option PackagesUpdateOption() =>
            new Option<bool>(
                aliases: new[] { "--packages-update" },
                description: "Allow package updates for dotnet app to work with Azure AD/AD B2C app (setup authentication).")
            {
                IsRequired = false
            };

        private static Option ClientIdOption() =>
            new Option<string>(
                aliases: new[] { "--client-id" },
                description: "Client ID of an existing app registration to use." +
                             "\n\tConfigure project to use an existing Azure app registration instead of creating a new one. The app registration will be updated if needed." +
                             "\n\t- When using this option, you may also need to pass in the client secret with --client-secret.\n")
            {
                IsRequired = false
            };

        private static Option AppDisplayName() =>
            new Option<string>(
                aliases: new[] { "--app-display-name" },
                description: "App display name for Azure AD/AD B2C app registration creation.\n")
            {
                IsRequired = false
            };

        private static Option ProjectType() =>
            new Option<string>(
                aliases: new[] { "--project-type" },
                description: "Project type for which to register the azure ad app registration." +
                             "\n\tFor eg., 'webapp', 'webapi', 'blazorwasm-hosted', 'blazorwasm'\n")
            {
                IsRequired = false
            };

        private static Option ClientSecretOption() =>
            new Option<string>(
                aliases: new[] { "--client-secret" },
                description: "Value for the client secret, which also can be referred to as application password.\n")
            {
                IsRequired = false
            };

        private static Option RedirectUriOption() =>
            new Option<IList<string>>(
                aliases: new[] { "--redirect-uris" },
                description: "Add redirect URIs (web) for the app.\n\t- You can pass in multiple values for this parameter separated by a space.\n")
            {
                IsRequired = false
            };

        private static Option ProjectFilePathOption() =>
            new Option<string>(
                aliases: new[] { "-p", "--project-file-path" },
                description: "Path to the project file (.csproj file) to be used. If not provided, the project file in the current working directory will be used.\n")
            {
                IsRequired = false
            };

        private static Option AppIdUriOption() =>
            new Option<string>(
                aliases: new[] { "--app-id-uri" },
                description: "The App ID Uri for the Blazor WebAssembly hosted API. This parameter will only be used for Blazor WebAssembly hosted applications.\n")
            {
                IsRequired = false
            };

        private static Option ApiClientIdOption() =>
            new Option<string>(
                aliases: new[] { "--api-client-id" },
                description: "Client ID of the Blazor WebAssembly hosted web API." +
                             "\nThis parameter is only used for Blazor WebAssembly hosted applications, where you only want to configure the project.\n")
            {
                IsRequired = false
            };
        private static Option SusiPolicyIdOption() =>
            new Option<string>(
                aliases: new[] { "--susi-policy-id" },
                description: "Sign-up/Sign-in policy required for configurating a B2C application from code that was created for Azure AD.\n")
            {
                IsRequired = false
            };
        private static Option TenantOption() =>
            new Option<string>(
                aliases: new[] { "-t", "--tenant-id" },
                description: "Azure AD or Azure AD B2C tenant in which to create or update the app registration.\n - If specified, the app registration will be created in given tenant.\n - If not provided, the app registration will be created in yuor home tenant.\n")
            {
                IsRequired = false
            };

        private static Option UsernameOption() =>
            new Option<string>(
                aliases: new[] { "-u", "--username" },
                description: "Username to use to connect to the Azure AD or Azure AD B2C tenant." +
                            "\n- It's only needed if you are signed-in to Visual Studio, or Azure CLI with several identities." +
                            "\n- In that case, the username parameter is used to determine which identity to use to create the app registration in the tenant.\n")
            {
                IsRequired = false
            };
    }
}
