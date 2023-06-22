# `dotnet msidentity`

Command line tool to update existing ASP.NET Core apps to use the Microsoft identity platform for authentication and authorization. When executed, this tool
will update the application code and create, or update, applications in an Azure AD, or AD B2C, tenant.

## Installing/Uninstalling the latest published version

To install the `dotnet msidentity` tool, we will use the [`dotnet tool`](https://docs.microsoft.com/en-us/dotnet/core/tools/dotnet-tool-install) command.
This command will download the specified NuGet package, and then install the tool that is contained in that package. Since the `dotnet msidentity` tool is currently
in preview, you will need to specify the `--version` parameter when calling `dotnet tool install`. To install the latest version of the tool, run the following.

```Shell
  dotnet tool install Microsoft.dotnet-msidentity -g --version "1.0.2"
```

To install a different version of the tool, you can find the available versions at https://www.nuget.org/packages/Microsoft.dotnet-msidentity.

To uninstall the tool execute the following.

```Shell
  dotnet tool uninstall -g microsoft.dotnet-msidentity
```

## Installing/Uninstalling the tool from the repo

To build the `dotnet msidentity` code and install it as a .NET Core global tool. Follow the steps below in a console.

### Windows
 1. `cd` to the repo root directory
 1. `scripts\install-msidentity.cmd`

### MacOS or Linux
 1. `cd` to the repo root directory
 1. `scripts/install-msidentity.sh`

If the script successfully completes, the `dotnet msidentity` tool is available as a global tool.

To uninstall the tool (from any directory).

```Shell
  dotnet tool uninstall --global Microsoft.dotnet-msidentity
```

## Pre-requisites to using the tool

Have an Azure AD or AD B2C tenant (or both). 
- If you want to add an Azure AD registration, you are usually already signed-in in Visual Studio in a tenant. If needed you can create your own tenant by following this quickstart [Setup a tenant](https://docs.microsoft.com/azure/active-directory/develop/quickstart-create-new-tenant). But be sure to sign-out and sign-in from Visual Studio or Azure CLI so that this tenant is known in the shared token cache.

- If you want to add an Azure AD B2C registration you'll need a B2C tenant, and explicitly pass it to the `--tenant-id` option of the tool. As well as the sign-up/sign-in policy `--susi-policy-id`. To create a B2C tenant, see [Create a B2C tenant](https://docs.microsoft.com/azure/active-directory-b2c/tutorial-create-tenant).

## Using the tool

To create an ASP.NET Core project that has auth support with the Microsoft identity platform requires two steps on the command line.
 
 1. Create the ASP.NET Core project with `dotnet new` and pass the parameter `--auth` parameter.
 2. Update auth for the project with the `dotnet msidentity` tool.

```text
dotnet-msidentity:
  Creates or updates an Azure AD / AD B2C app registration, and updates the project, using
   your developer credentials (from Visual Studio, Azure CLI, Azure RM PowerShell, VS Code).
   Use this tool in folders containing applications created with the following command:

   dotnet new <template> --auth <authOption> [--calls-graph] [--called-api-url <URI> --called-api-scopes <scopes>]

   where the <template> is a webapp, mvc, webapi, blazorserver, blazorwasm.
   See https://aka.ms/dotnet-msidentity.

Usage:
  dotnet msidentity [command] [options]

Commands:
  --register-app               Registers or updates an Azure AD or Azure AD B2C App registration in Azure.
                                        - Updates the appsettings.json file.
                                        - Updates local user secrets
                                        - Updates Startup.cs and package references if needed.

  --unregister-app             Unregister an Azure AD or Azure AD B2C Application in Azure.
  
  --update-app-registration    Update an Azure AD or Azure AD B2C app registration in Azure.

Internal Commands (These commands have little do with registering Azure AD/Azure AD B2C apps but are nice helpers):
  
  --list--aad-apps                     Lists Azure AD Applications for a given tenant + username.
  
  --list-service-principals            Lists Azure AD Service Principals for a given tenant + username.
  
  --list-tenants                       Lists Azure AD + Azure AD B2C tenants for a given username.
  
  --update-project                     Given client id for an Azure AD/AD B2C app, update appsettings.json, local user secrets. [TODO : and project code(Startup.cs, project references to get the app auth ready).]
  
  --create-client-secret               Create a client secret for given app registration (client id) and print the secret.
  
  --create-app-registration            Create an Azure AD or Azure AD B2C app registration in Azure.    

Options:
  --tenant-id <tenant-id>              Azure AD or Azure AD B2C tenant in which to create/update the app.
                                        - If specified, the tool will create the application in the specified tenant.
                                        - Otherwise it will create the app in your home tenant.
  
  --username <username>                Username to use to connect to the Azure AD or Azure AD B2C tenant.
                                        It's only needed when you are signed-in in Visual Studio, or Azure CLI with
                                        several identities. In that case, the username param is used to disambiguate
                                        which identity to use to create the app in the tenant.
  
  --client-id <client-id>              Client ID of an existing application from which to update the code. This is
                                        used when you don't want to register a new app, but want to configure the code
                                        from an existing application (which can also be updated by the tool if needed).
                                        You might want to also pass-in the if you know it.
  
  -p, --project-file-path              Path to the project file (.csproj file) to be used. 
   <project-file-path>                  If not provided, the project file in the current working directory will be used.
  
  --client-secret <client-secret>      Client secret to use as a client credential.
  
  --susi-policy-id <susi-policy-id>    Sign-up/Sign-in policy required for configurating
                                        a B2C application from code that was created for Azure AD.
  
  --api-client-id <api-client-id>      Client ID of the blazorwasm hosted web API.
                                        This is only used on the case of a blazorwasm hosted application where you only
                                        want to configure the code (named after the --api-client-id blazorwasm
                                        template parameter).
  
  --app-id-uri <app-id-uri>            The App ID Uri for the blazorwasm hosted API. It's only used
                                        on the case of a blazorwasm hosted application (named after the --app-id-uri
                                        blazorwasm template parameter).
  
  --version                            Display the version of this tool.
  
  -?, -h, --help                       Show commandline help.
```

If you use PowerShell, or Bash, you can also get the completion in the shell, provivided you install [dotnet-suggest](https://www.nuget.org/packages/dotnet-suggest/). See https://github.com/dotnet/command-line-api/blob/main/docs/dotnet-suggest.md on how to configure the shell so that it leverages dotnet-suggest.

## Scenarios

### Registering a new Azure AD app and configuring the code using your dev credentials

Given existing code which is not yet configured:

- detects the kind of application (web app, web api, Blazor server, Blazor web assembly, hosted or not)
- detects the Identity Provider (IdP) (Azure AD or B2C)
- creates a new app registration in the tenant, using your developer credentials if possible (and prompting you otherwise). Ensures redirect URIs are registered for all `applicationUrl`s listed in the `launchSettings.json` file.
- updates the configuration files (and program.cs for Blazor apps)

Note that in the following samples, you can always have your templates add a call to Microsoft graph [--calls-graph], or to a downstream API [--called-api-url URI --called-api-scopes scopes]. This is now shown here to keep things simple.

- Creates a new app in your home tenant and updates code
```
dotnet new webapp --auth SingleOrg
dotnet msidentity --register-application
```

- Creates a new app in a different tenant and updates code
```
dotnet new webapp --auth SingleOrg
dotnet msidentity --register-app --tenant-id testprovisionningtool.onmicrosoft.com
```

- Creates a new app using a different identity and updates code
```
dotnet new webapp --auth SingleOrg
dotnet msidentity --register-app --username username@domain.com
```

 ### Registering a new AzureAD B2C app and configuring the code using your dev credentials

Note that in the following samples, you can always have your templates add a call to Microsoft graph [--calls-graph], or to a downstream API [--called-api-url URI --called-api-scopes scopes]. This is now shown here to keep things simple.

- Creates a new Azure AD B2C app and updates code which was initially meant to be for Azure AD.
```
dotnet new webapp --auth SingleOrg
dotnet msidentity --register-app --tenant-id fabrikamb2c.onmicrosoft.com --susi-policy-id b2c_1_susi
```

- Creates a new Azure AD B2C app and updates code
```
dotnet new webapp --auth IndividualB2C
dotnet msidentity --register-app --tenant-id fabrikamb2c.onmicrosoft.com
```

- Creates a new app Azure AD B2C app using a different identity and updates code
```
dotnet new webapp --auth IndividualB2C
dotnet msidentity --register-app --tenant-id fabrikamb2c.onmicrosoft.com  --username username@domain.com
```


 ### Configuring code from an existing application
 
 The following configures code with an existing application.

 ```Shell
dotnet new webapp --auth SingleOrg

dotnet msidentity --register-app [--tenant-id <tenantId>] --client-id <clientId>
 ```

 Same thing for an application calling Microsoft Graph

 ```Shell
dotnet new webapp --auth SingleOrg --calls-graph

dotnet msidentity --register-app [--tenant-id <tenantId>] --client-id <clientId>
 ```

### Updating an existing project which is not configured to use Azure AD or Azure AD B2C

This scenario is not currently supported, but it is planned to be implemented. Currently this tool can only update projects which are already
configured for Azure AD or Azure AD B2C.

## Supported frameworks

The tool supports ASP.NET Core applications created with .NET 5.0 and netcoreapp3.1. In the case of netcoreapp3.1, for blazorwasm applications, the redirect URI created for the app is a "Web" redirect URI (as Blazor web assembly leverages MSAL.js 1.x in netcoreapp3.1), whereas in net5.0 it's a "SPA" redirect URI (as Blazor web assembly leverages MSAL.js 2.x in net5.0) 

```Shell
dotnet new blazorwasm --auth SingleOrg --framework netcoreapp3.1
dotnet msidentity
dotnet run --framework netstandard2.1
```
