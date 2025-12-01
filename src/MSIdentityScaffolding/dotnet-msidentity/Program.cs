// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.DotNet.MSIdentity.Shared;
using System.Collections.Generic;
using System.CommandLine;
using System.CommandLine.Parsing;
using System.Threading.Tasks;

namespace Microsoft.DotNet.MSIdentity.Tool
{
    public static class Program
    {
        public static async Task<int> Main(string[] args)
        {
            var rootCommand = MsIdentityCommand();
            //new BinderBase for System.Commandline update, new way to bind handlers to commands.
            var provisioningToolBinder = new ProvisioningToolOptionsBinder(
                JsonOption,
                EnableIdTokenOption,
                EnableAccessToken,
                CallsGraphOption,
                CallsDownstreamApiOption,
                UpdateUserSecretsOption,
                ConfigUpdateOption,
                CodeUpdateOption,
                PackagesUpdateOption,
                ClientIdOption,
                AppDisplayName,
                ProjectType,
                ClientSecretOption,
                RedirectUriOption,
                ProjectFilePathOption,
                ClientProjectOption,
                ApiScopesOption,
                HostedAppIdUriOption,
                ApiClientIdOption,
                SusiPolicyIdOption,
                TenantOption,
                UsernameOption,
                InstanceOption,
                CalledApiUrlOption);

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
            listAadAppsCommand.Hidden = true;
            listServicePrincipalsCommand.Hidden = true;
            listTenantsCommand.Hidden = true;
            updateProjectCommand.Hidden = true;
            createClientSecretCommand.Hidden = true;

            //add all commands to root command.
            rootCommand.Subcommands.Add(listAadAppsCommand);
            rootCommand.Subcommands.Add(listServicePrincipalsCommand);
            rootCommand.Subcommands.Add(listTenantsCommand);
            rootCommand.Subcommands.Add(registerApplicationCommand);
            rootCommand.Subcommands.Add(unregisterApplicationCommand);
            rootCommand.Subcommands.Add(updateAppRegistrationCommand);
            rootCommand.Subcommands.Add(updateProjectCommand);
            rootCommand.Subcommands.Add(createClientSecretCommand);
            rootCommand.Subcommands.Add(createAppRegistration);

            //if no args are present, show default help.
            if (args == null || args.Length == 0)
            {
                args = new string[] { "-h" };
            }

            listAadAppsCommand.SetAction((parseResult, cancellationToken) => HandleListApps(provisioningToolBinder.GetBoundValue(parseResult)));
            listServicePrincipalsCommand.SetAction((parseResult, cancellationToken) => HandleListServicePrincipals(provisioningToolBinder.GetBoundValue(parseResult)));
            listTenantsCommand.SetAction((parseResult, cancellationToken) => HandleListTenants(provisioningToolBinder.GetBoundValue(parseResult)));
            registerApplicationCommand.SetAction((parseResult, cancellationToken) => HandleRegisterApplication(provisioningToolBinder.GetBoundValue(parseResult)));
            unregisterApplicationCommand.SetAction((parseResult, cancellationToken) => HandleUnregisterApplication(provisioningToolBinder.GetBoundValue(parseResult)));
            updateAppRegistrationCommand.SetAction((parseResult, cancellationToken) => HandleUpdateApplication(provisioningToolBinder.GetBoundValue(parseResult)));
            updateProjectCommand.SetAction((parseResult, cancellationToken) => HandleUpdateProject(provisioningToolBinder.GetBoundValue(parseResult)));
            createClientSecretCommand.SetAction((parseResult, cancellationToken) => HandleClientSecrets(provisioningToolBinder.GetBoundValue(parseResult)));
            createAppRegistration.SetAction((parseResult, cancellationToken) => HandleCreateAppRegistration(provisioningToolBinder.GetBoundValue(parseResult)));

            ParseResult parseResult = rootCommand.Parse(args);
            return await parseResult.InvokeAsync();
        }

        internal static async Task<int> HandleListApps(ProvisioningToolOptions provisioningToolOptions)
        {
            if (provisioningToolOptions != null)
            {
                IMsAADTool msAADTool = MsAADToolFactory.CreateTool(Commands.LIST_AAD_APPS_COMMAND, provisioningToolOptions);
                await msAADTool.Run();
                return 0;
            }
            return -1;
        }

        internal static async Task<int> HandleListTenants(ProvisioningToolOptions provisioningToolOptions)
        {
            if (provisioningToolOptions != null)
            {
                IMsAADTool msAADTool = MsAADToolFactory.CreateTool(Commands.LIST_TENANTS_COMMAND, provisioningToolOptions);
                await msAADTool.Run();
                return 0;
            }
            return -1;
        }

        internal static async Task<int> HandleListServicePrincipals(ProvisioningToolOptions provisioningToolOptions)
        {
            if (provisioningToolOptions != null)
            {
                IMsAADTool msAADTool = MsAADToolFactory.CreateTool(Commands.LIST_SERVICE_PRINCIPALS_COMMAND, provisioningToolOptions);
                await msAADTool.Run();
                return 0;
            }
            return -1;
        }

        internal static async Task<int> HandleRegisterApplication(ProvisioningToolOptions provisioningToolOptions)
        {
            if (provisioningToolOptions != null)
            {
                IMsAADTool msAADTool = MsAADToolFactory.CreateTool(Commands.REGISTER_APPLICATIION_COMMAND, provisioningToolOptions);
                await msAADTool.Run();
                return 0;
            }
            return -1;
        }

        internal static async Task<int> HandleUpdateApplication(ProvisioningToolOptions provisioningToolOptions)
        {
            if (provisioningToolOptions != null)
            {
                IMsAADTool msAADTool = MsAADToolFactory.CreateTool(Commands.UPDATE_APP_REGISTRATION_COMMAND, provisioningToolOptions);
                await msAADTool.Run();
                return 0;
            }
            return -1;
        }

        internal static async Task<int> HandleUnregisterApplication(ProvisioningToolOptions provisioningToolOptions)
        {
            if (provisioningToolOptions != null)
            {
                IMsAADTool msAADTool = MsAADToolFactory.CreateTool(Commands.UNREGISTER_APPLICATION_COMMAND, provisioningToolOptions);
                await msAADTool.Run();
                return 0;
            }
            return -1;
        }

        internal static async Task<int> HandleCreateAppRegistration(ProvisioningToolOptions provisioningToolOptions)
        {
            if (provisioningToolOptions != null)
            {
                IMsAADTool msAADTool = MsAADToolFactory.CreateTool(Commands.CREATE_APP_REGISTRATION_COMMAND, provisioningToolOptions);
                await msAADTool.Run();
                return 0;
            }
            return -1;
        }

        internal static async Task<int> HandleUpdateProject(ProvisioningToolOptions provisioningToolOptions)
        {
            if (provisioningToolOptions != null)
            {
                IMsAADTool msAADTool = MsAADToolFactory.CreateTool(Commands.UPDATE_PROJECT_COMMAND, provisioningToolOptions);
                await msAADTool.Run();
                return 0;
            }
            return -1;
        }

        internal static async Task<int> HandleClientSecrets(ProvisioningToolOptions provisioningToolOptions)
        {
            if (provisioningToolOptions != null)
            {
                IMsAADTool msAADTool = MsAADToolFactory.CreateTool(Commands.ADD_CLIENT_SECRET, provisioningToolOptions);
                await msAADTool.Run();
                return 0;
            }
            return -1;
        }

        internal static RootCommand MsIdentityCommand() =>
            new(
                description: "Creates or updates an Azure AD / Azure AD B2C application, and updates the project, using your developer credentials (from Visual Studio, Azure CLI, Azure RM PowerShell, VS Code).\n")
            {
            };

        internal static Command ListAADAppsCommand() =>
            new(
                name: Commands.LIST_AAD_APPS_COMMAND,
                description: "Lists AAD Applications for a given tenant/username.\n")
            {
                TenantOption, UsernameOption, InstanceOption, JsonOption
            };

        internal static Command ListServicePrincipalsCommand() =>
            new(
                name: Commands.LIST_SERVICE_PRINCIPALS_COMMAND,
                description: "Lists AAD Service Principals.\n")
            {
                TenantOption, UsernameOption, InstanceOption, JsonOption
            };

        internal static Command ListTenantsCommand() =>
            new(
                name: Commands.LIST_TENANTS_COMMAND,
                description: "Lists AAD and AAD B2C tenants for a given user.\n")
            {
                UsernameOption, JsonOption
            };

        internal static Command CreateClientSecretCommand() =>
            new(
                name: Commands.ADD_CLIENT_SECRET,
                description: "Create client secret for an Azure AD or AD B2C app registration.\n")
            {
                TenantOption, UsernameOption, InstanceOption, JsonOption, ClientIdOption, ProjectFilePathOption, UpdateUserSecretsOption
            };

        internal static Command RegisterApplicationCommand() =>
            new(
                name: Commands.REGISTER_APPLICATIION_COMMAND,
                description: "Register an Azure AD or Azure AD B2C app registration in Azure and update the project." +
                             "\n\t- Updates the appsettings.json file.\n")
            {
                TenantOption, UsernameOption, InstanceOption, JsonOption, ClientIdOption, ClientSecretOption, HostedAppIdUriOption, ApiClientIdOption, SusiPolicyIdOption, ProjectFilePathOption
            };

        internal static Command UpdateProjectCommand() =>
            new(
                name: Commands.UPDATE_PROJECT_COMMAND,
                description: "Update an Azure AD/AD B2C app registration in Azure and the project." +
                             "\n\t- Updates the appsettings.json file." +
                             "\n\t- Updates the Startup.cs file." +
                             "\n\t- Updates the user secrets.\n")
            {
                TenantOption, UsernameOption, InstanceOption, ClientIdOption, JsonOption, ProjectFilePathOption, ConfigUpdateOption, CodeUpdateOption, PackagesUpdateOption, CallsGraphOption, CallsDownstreamApiOption, UpdateUserSecretsOption, RedirectUriOption, SusiPolicyIdOption, CalledApiUrlOption, ApiScopesOption
            };

        internal static Command UpdateAppRegistrationCommand() =>
            new(
                name: Commands.UPDATE_APP_REGISTRATION_COMMAND,
                description: "Update an Azure AD/AD B2C app registration in Azure.\n")
            {
                TenantOption, UsernameOption, InstanceOption, JsonOption, HostedAppIdUriOption, ClientIdOption, RedirectUriOption, EnableIdTokenOption, EnableAccessToken, ClientProjectOption, ApiScopesOption
            };

        internal static Command CreateAppRegistrationCommand() =>
            new(
                name: Commands.CREATE_APP_REGISTRATION_COMMAND,
                description: "Create an Azure AD/AD B2C app registration in Azure.\n")
            {
                TenantOption, UsernameOption, InstanceOption, JsonOption, AppDisplayName, ProjectFilePathOption, ProjectType, ClientProjectOption
            };

        internal static Command UnregisterApplicationCommand() =>
            new(
                name: Commands.UNREGISTER_APPLICATION_COMMAND,

                description: "Unregister an Azure AD or Azure AD B2C app registration in Azure." +
                             "\n\t- Updates the appsettings.json file.\n")
            {
                TenantOption, UsernameOption, InstanceOption, JsonOption, HostedAppIdUriOption, ProjectFilePathOption, ClientIdOption
            };

        internal static Option<bool> JsonOption { get; } = CreateJsonOption();

        private static Option<bool> CreateJsonOption()
        {
            var option = new Option<bool>("--json", "-j")
            {
                Description = "Format output in JSON instead of text.\n"
            };
            return option;
        }

        internal static Option<bool> EnableIdTokenOption { get; } =
            new("--enable-id-token")
            {
                Description = "Enable id token.\n"
            };

        internal static Option<bool> EnableAccessToken { get; } =
            new("--enable-access-token")
            {
                Description = "Enable access token.\n"
            };

        internal static Option<bool> CallsGraphOption { get; } =
            new("--calls-graph")
            {
                Description = "App registration calls microsoft graph.\n"
            };

        internal static Option<bool> CallsDownstreamApiOption { get; } =
            new("--calls-downstream-api")
            {
                Description = "App registration calls downstream api.\n"
            };

        internal static Option<bool> UpdateUserSecretsOption { get; } =
            new("--update-user-secrets")
            {
                Description = "Add secrets to user secrets.json file." +
                             "\n\t- Using dotnet-user-secrets to init and set user secrets.\n"
            };

        internal static Option<bool> ConfigUpdateOption { get; } =
            new("--config-update")
            {
                Description = "Allow config changes for dotnet app to work with Azure AD/AD B2C app."
            };

        internal static Option<bool> CodeUpdateOption { get; } =
            new("--code-update")
            {
                Description = "Allow Startup.cs and other code changes for dotnet app to work with Azure AD/AD B2C app (setup authentication)."
            };

        internal static Option<bool> PackagesUpdateOption { get; } =
            new("--packages-update")
            {
                Description = "Allow package updates for dotnet app to work with Azure AD/AD B2C app (setup authentication)."
            };

        internal static Option<string> ClientIdOption { get; } =
            new("--client-id")
            {
                Description = "Client ID of an existing app registration to use." +
                             "\n\tConfigure project to use an existing Azure app registration instead of creating a new one. The app registration will be updated if needed." +
                             "\n\t- When using this option, you may also need to pass in the client secret with --client-secret.\n"
            };

        internal static Option<string> AppDisplayName { get; } =
            new("--app-display-name")
            {
                Description = "App display name for Azure AD/AD B2C app registration creation.\n"
            };

        internal static Option<string> ProjectType { get; } =
            new("--project-type")
            {
                Description = "Project type for which to register the azure ad app registration." +
                             "\n\tFor eg., 'webapp', 'webapi', 'blazorwasm-hosted', 'blazorwasm'\n"
            };

        internal static Option<string> ClientSecretOption { get; } =
            new("--client-secret")
            {
                Description = "Value for the client secret, which also can be referred to as application password.\n"
            };

        internal static Option<IList<string>> RedirectUriOption { get; } =
            new("--redirect-uris")
            {
                Description = "Add redirect URIs (web) for the app.\n\t- You can pass in multiple values for this parameter separated by a space.\n",
                AllowMultipleArgumentsPerToken = true
            };

        internal static Option<string> ProjectFilePathOption { get; } = CreateProjectFilePathOption();

        private static Option<string> CreateProjectFilePathOption()
        {
            var option = new Option<string>("--project-file-path", "-p")
            {
                Description = "Path to the project file (.csproj file) to be used. If not provided, the project file in the current working directory will be used.\n"
            };
            return option;
        }

        internal static Option<string> ClientProjectOption { get; } =
            new("--client-project")
            {
                Description = "Path to the project file (.csproj file) for a hosted Blazor WASM client. If provided, implies that project is Blazor WASM Hosted\n"
            };

        internal static Option<string> ApiScopesOption { get; } =
            new("--api-scopes")
            {
                Description = "Scopes for the called downstream API, especially useful for B2C scenarios where permissions must be granted manually\n"
            };

        internal static Option<string> CalledApiUrlOption { get; } =
            new("--called-api-url")
            {
                Description = "URL of the called downstream API\n"
            };

        internal static Option<string> HostedAppIdUriOption { get; } =
            new("--hosted-app-id-uri")
            {
                Description = "The App ID Uri for the Blazor WebAssembly hosted API. This parameter will only be used for Blazor WebAssembly hosted applications.\n"
            };

        internal static Option<string> ApiClientIdOption { get; } =
            new("--api-client-id")
            {
                Description = "Client ID of the Blazor WebAssembly hosted web API." +
                             "\nThis parameter is only used for Blazor WebAssembly hosted applications, where you only want to configure the project.\n"
            };

        internal static Option<string> SusiPolicyIdOption { get; } =
            new("--susi-policy-id")
            {
                Description = "Sign-up/Sign-in policy required for configurating a B2C application from code that was created for Azure AD.\n"
            };

        internal static Option<string> TenantOption { get; } = CreateTenantOption();

        private static Option<string> CreateTenantOption()
        {
            var option = new Option<string>("--tenant-id", "-t")
            {
                Description = "Azure AD or Azure AD B2C tenant in which to create or update the app registration.\n - If specified, the app registration will be created in given tenant.\n - If not provided, the app registration will be created in yuor home tenant.\n"
            };
            return option;
        }

        internal static Option<string> UsernameOption { get; } = CreateUsernameOption();

        private static Option<string> CreateUsernameOption()
        {
            var option = new Option<string>("--username", "-u")
            {
                Description = "Username to use to connect to the Azure AD or Azure AD B2C tenant." +
                            "\n- It's only needed if you are signed-in to Visual Studio, or Azure CLI with several identities." +
                            "\n- In that case, the username parameter is used to determine which identity to use to create the app registration in the tenant.\n"
            };
            return option;
        }

        internal static Option<string> InstanceOption { get; } = CreateInstanceOption();

        private static Option<string> CreateInstanceOption()
        {
            var option = new Option<string>("--instance", "-i")
            {
                Description = "Instance where the Azure AD or Azure AD B2C tenant is located.\n" +
                "If not specified, will default to https://login.microsoftonline.com/"
            };
            return option;
        }
    }
}
