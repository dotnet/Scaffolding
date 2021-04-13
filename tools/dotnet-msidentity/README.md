# dotnet-msidentity
Command line tool that creates Microsoft identity platform applications in a tenant (AAD or B2C) and updates the configuration code of your ASP.NET Core applications (mvc, webapp, blazorwasm, blazorwasm hosted, blazorserver). The tool can also be used to update code from an existing AAD/AAD B2C application.

## Installing/Uninstalling the release/prerelease tool 
Install the prerelease 1.0.0-Preview 1 version of the dotnet-msidentity tool (as a global tool) :
  dotnet tool install Microsoft.dotnet-msidentity -g --version "1.0.0-preview.1.21212.1"

## Installing/Uninstalling the tool from the repo
Use the global_install.cmd global_install.sh command to install the package. 
- Edit Version.props to match the version in global_install.

If later you want to uninstall the tool, just run (from anywhere):
```Shell
dotnet tool uninstall --global dotnet-msidentity
```

## Pre-requisites to using the tool

Have an AAD or B2C tenant (or both). 
- If you want to add an AAD registration, you are usually already signed-in in Visual Studio in a tenant. If needed you can create your own tenant by following this quickstart [Setup a tenant](https://docs.microsoft.com/azure/active-directory/develop/quickstart-create-new-tenant). But be sure to sign-out and sign-in from Visual Studio or Azure CLI so that this tenant is known in the shared token cache.

- If you want to add an AAD B2C registration you'll need a B2C tenant, and explicity pass it to the `--tenant-id` option of the tool. As well as the sign-up/sign-in policy `--susi-policy-id`. To create a B2C tenant, see [Create a B2C tenant](https://docs.microsoft.com/azure/active-directory-b2c/tutorial-create-tenant).

## Using the tool

```text
dotnet-msidentity:
  Creates or updates an Azure AD / Azure AD B2C application, and updates the code, using
   your developer credentials (from Visual Studio, Azure CLI, Azure RM PowerShell, VS Code).
   Use this tool in folders containing applications created with the following command:

   dotnet new <template> --auth <authOption> [--calls-graph] [--called-api-url <URI> --called-api-scopes <scopes>]

   where the <template> is a webapp, mvc, webapi, blazorserver, blazorwasm.
   See https://aka.ms/dotnet-msidentity.

Usage:
  dotnet-msidentity [command] [options]

Commands:
  --register-app               Registers/updates an AAD/AAD B2C Application in Azure.
                                        - Updates the appsettings.json file.
                                        - Updates local user secrets
  --unregister-app             Unregister an AAD/AAD B2C Application in Azure.

Internal Commands (These commands have little do with registering AAD/AAD B2C apps but are nice helpers):
  --list--aad-apps                     Lists AAD Applications for a given tenant + username.
  --list-service-principals            Lists AAD Service Principals for a given tenant + username.
  --list-tenants                       Lists AAD + AAD B2C tenants for a given username.
  --update-project                     Given client id for an Azure AD/AD B2C app, update appsettings.json, local user secrets. [TODO : and project code(Startup.cs, project references to get the app auth ready).]

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
  --folder <folder>                    When specified, will analyze the application code in the specified folder.
                                        Otherwise analyzes the code in the current directory.
  --client-secret <client-secret>      Client secret to use as a client credential.
  --susi-policy-id <susi-policy-id>    Sign-up/Sign-in policy required for configurating
                                        a B2C application from code that was created for AAD.
  --api-client-id <api-client-id>      Client ID of the blazorwasm hosted web API.
                                        This is only used on the case of a blazorwasm hosted application where you only
                                        want to configure the code (named after the --api-client-id blazorwasm
                                        template parameter).
  --app-id-uri <app-id-uri>            The App ID Uri for the blazorwasm hosted API. It's only used
                                        on the case of a blazorwasm hosted application (named after the --app-id-uri
                                        blazorwasm template parameter).
  --unregister                         Unregister the application, instead of registering it.
  --version                            Show version information
  -?, -h, --help                       Show help and usage information
```

If you use PowerShell, or Bash, you can also get the completion in the shell, provivided you install [dotnet-suggest](https://www.nuget.org/packages/dotnet-suggest/). See https://github.com/dotnet/command-line-api/blob/main/docs/dotnet-suggest.md on how to configure the shell so that it leverages dotnet-suggest.

## Scenarios


### Registering a new AAD app and configuring the code using your dev credentials

Given existing code which is not yet configured: 
- detects the kind of application (web app, web api, blazor server, blazor web assembly, hosted or not)
- detects the IDP (AAD or B2C*)
- creates a new app registration in the tenant, using your developer credentials if possible (and prompting you otherwise). Ensures redirect URIs are registered for all the launchsettings ports.
- updates the configuration files (and program.cs for Blazor apps)

Note that in the following samples, you can always have your templates adding a calls to Microsoft graph [--calls-graph], or to a downstream API [--called-api-url URI --called-api-scopes scopes]. This is now shown here to keep things simple.

<table>
 <tr>
  <td>
   <code>
dotnet new webapp --auth SingleOrg

dotnet-msidentity --register-application
   </code>
  </td>
  <td>Creates a new app <b>in your home tenant</b> and updates code</td>
 </tr>
 
 <tr>
  <td>
   <code>
dotnet new webapp --auth SingleOrg

dotnet-msidentity --register-application --tenant-id testprovisionningtool.onmicrosoft.com
   </code>
  </td>
  <td>Creates a new app <b>in a different tenant</b> and updates code</td>
 </tr> 
 
  <tr>
  <td>
   <code>
dotnet new webapp --auth SingleOrg

dotnet-msidentity --register-application --username username@domain.com
   </code>
  </td>
  <td>Creates a new app <b>using a different identity</b> and updates code</td>
 </tr> 
 
 </table>
 
 ### Registering a new AzureAD B2C app and configuring the code using your dev credentials

Note that in the following samples, you can always have your templates adding a calls to a downstream API [--called-api-url URI --called-api-scopes scopes]. This is now shown here to keep things simple.

<table>
 <tr>
  <td>
   <code>
dotnet new webapp --auth SingleOrg

dotnet-msidentity --register-application --tenant-id fabrikamb2c.onmicrosoft.com --susi-policy-id b2c_1_susi
   </code>
  </td>
  <td>Creates a new Azure AD B2C app and updates code which was initially meant
  to be for Azure AD.</td>
 </tr> 

  <tr>
  <td>
   <code>
dotnet new webapp --auth IndividualB2C

dotnet-msidentity --register-application --tenant-id fabrikamb2c.onmicrosoft.com
   </code>
  </td>
  <td>Creates a new Azure AD B2C app and updates code</td>
 </tr> 
 
  <tr>
  <td>
   <code>
dotnet new webapp --auth IndividualB2C

dotnet-msidentity --register-application --tenant-id fabrikamb2c.onmicrosoft.com  --username username@domain.com
   </code>
  </td>
  <td>Creates a new app Azure AD B2C app <b>using a different identity</b> and updates code</td>
 </tr> 
 
 </table>
 
 
 ### Configuring code from an existing application
 
 The following configures code with an existing application.

 ```Shell
dotnet new webapp --auth SingleOrg

dotnet-msidentity --register-application [--tenant-id <tenantId>] --client-id <clientId>
 ```

 Same thing for an application calling Microsoft Graph

 ```Shell
dotnet new webapp --auth SingleOrg --calls-graph

dotnet-msidentity --register-application [--tenant-id <tenantId>] --client-id <clientId>
 ```

 ### Adding code and configuration to an app which is not authentication/authorization enabled yet
 
 This scenario is on the backlog, but not yet supported

## Supported frameworks

The tool supports ASP.NET Core applications created with .NET 5.0 and netcoreapp3.1. In the case of netcoreapp3.1, for blazorwasm applictions, the redirect URI created for the app is a "Web" redirect URI (as Blazor web assembly leverages MSAL.js 1.x in netcoreapp3.1), whereas in net5.0 it's a "SPA" redirect URI (as Blazor web assembly leverages MSAL.js 2.x in net5.0) 

```Shell
dotnet new blazorwasm --auth SingleOrg --framework netcoreapp3.1
dotnet-msidentity
dotnet run -f netstandard2.1
```
