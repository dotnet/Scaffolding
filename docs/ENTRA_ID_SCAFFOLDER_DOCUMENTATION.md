# Microsoft Entra ID Scaffolder Documentation

## Table of Contents
- [Overview](#overview)
- [What is the Entra ID Scaffolder?](#what-is-the-entra-id-scaffolder)
- [Key Features](#key-features)
- [Prerequisites](#prerequisites)
- [Getting Started](#getting-started)
- [Command Usage](#command-usage)
- [Options Reference](#options-reference)
- [Common Scenarios](#common-scenarios)
- [What the Scaffolder Does](#what-the-scaffolder-does)
- [Supported Project Types](#supported-project-types)
- [Troubleshooting](#troubleshooting)
- [Best Practices](#best-practices)

---

## Overview

The **Microsoft Entra ID Scaffolder** is a command-line scaffolder (`dotnet scaffold aspnet entra-id`) that adds Microsoft Entra ID (formerly Azure AD) authentication to existing ASP.NET Core applications. The tool automates the process of:

- Registering or updating app registrations in Microsoft Entra ID
- Generating authentication-related code files
- Configuring local sign-in settings in `appsettings.Development.json`
- Adding necessary NuGet packages
- Modifying existing code to integrate authentication

---

## What is the Entra ID Scaffolder?

The Entra ID Scaffolder is part of the modern .NET scaffolding system that helps developers add Microsoft Entra ID authentication to their **existing** ASP.NET Core applications. Unlike tools that create new projects with authentication, this scaffolder retrofits authentication into projects that were created without it.

### Why Use It?

- **Add Auth to Existing Projects**: Quickly add Entra ID authentication to applications without starting over
- **Automates Complex Setup**: Handles app registration, code generation, and configuration automatically
- **Leverages Azure CLI**: Uses your existing Azure credentials from the Azure CLI
- **Reduces Errors**: Ensures consistent and correct authentication implementation
- **Blazor Focus**: Optimized for Blazor Server and Blazor WebAssembly applications
- **Interactive Experience**: Offers choices for creating new or selecting existing app registrations

---

## Key Features

### 1. **Automatic App Registration**
- Creates new app registrations in Microsoft Entra ID
- Updates existing app registrations when provided with a client ID
- Sets up sign-in only; does not automatically create client secrets or initialize `UserSecretsId` for new or existing registrations
- Preserves existing locally configured credentials
- Uses the underlying `dotnet msidentity` tool for Azure AD operations

When creating a new registration for a specific workforce tenant, the identity
tool defaults to single-tenant accounts (`AzureADMyOrg`) unless an audience is
explicitly supplied through its application parameters. Reusing an existing
registration does not change its audience. This default requires a version of
`dotnet msidentity` containing the single-tenant default fix; updating the
scaffolder alone does not update an already installed identity tool.

The scaffolder requires an identity tool that supports `--register-app
--registration-only`. This opt-in mode receives the validated project path and
explicit registration metadata; it does not infer a client ID from project
configuration or read/write `appsettings.json`. Other identity tool invocations
retain their existing project inference and update behavior. An explicitly
selected existing registration is still reused rather than creating a new one.

Generated local settings are saved only to `appsettings.Development.json`.
Atomic saves create an owner-only staging file before writing any content and
preserve the destination's Unix permissions or Windows DACL when replacing it.
New development settings files are owner-only. Preserved settings may contain
secrets; the staging file is cleaned up if writing or replacement fails.

### 2. **Code Generation**
- Generates authentication-related code files from templates
- Adds appropriate authentication middleware configuration
- Creates or updates `appsettings.Development.json` with local sign-in settings
- Leaves `appsettings.json` and user secrets unchanged

### 3. **Azure CLI Integration**
- Uses Azure CLI credentials for authentication
- Leverages your existing Azure login
- Retrieves tenant and application information dynamically

### 4. **Package Management**
- Automatically adds required NuGet packages for authentication
- Handles both standard and Blazor WebAssembly-specific packages
- Ensures correct package versions for your target framework

### 5. **Interactive Experience**
- Choose to create a new Azure AD application or select an existing one
- Validates inputs and provides helpful error messages
- Guides you through the authentication setup process

---

## Prerequisites

### Required Tools
- **.NET SDK**: Version **10.0 or later** (the Entra ID scaffolder is only available in .NET 10+)
  - Download from: https://dotnet.microsoft.com/download
- **dotnet scaffold tool**: Included with .NET 10+ SDK
- **Azure CLI**: Required for authentication and Azure operations
  - Install from: https://docs.microsoft.com/cli/azure/install-azure-cli
  - Must be logged in: `az login`

### Optional Tools
- **dotnet msidentity tool**: Used internally by the scaffolder
  - **Auto-installed**: The scaffolder will install this automatically if not present
  - Manual install: `dotnet tool install Microsoft.dotnet-msidentity -g`
  - No action needed unless you want to use it independently

### Azure Requirements
- **Entra ID Tenant**: Access to a Microsoft Entra ID (Azure AD) tenant
- **Permissions**: Ability to register applications in the tenant when creating a new registration, or update permission on the selected existing registration
- **Azure Subscription**: An active Azure subscription

### Project Requirements
- An **existing** ASP.NET Core project (.csproj file)
- Currently optimized for **Blazor** applications (Blazor Server and Blazor WebAssembly)
- Project must be targeting **.NET 10.0 or later**

### Before You Begin
1. Verify you have .NET 10 or later:
   ```bash
   dotnet --version
   # Should show 10.0.0 or higher
   ```

2. Ensure Azure CLI is installed and logged in:
   ```bash
   az login
   az account show
   ```

3. Have your tenant ID ready (find it in Azure Portal > Microsoft Entra ID > Overview)

**Note**: The `dotnet msidentity` tool will be automatically installed by the scaffolder if needed. You don't need to install it manually unless you plan to use it independently.

---

## Getting Started

### Development credentials and production deployment

New app registrations created by the scaffolder are for local development and testing.
Use a recognizable development-oriented display name for the registration. The
generated flow configures sign-in only: it does not create a client secret, initialize
`UserSecretsId`, or write credentials to user secrets. This applies to both new
registrations and `--use-existing-application true`; existing locally configured
credentials are left unchanged. Reusing a registration does not require permission
to create new app registrations.

Local sign-in settings are written only to `appsettings.Development.json`; base
`appsettings.json` is not read to select a registration or modified by this flow.
Run locally with the `Development` environment so these settings are loaded.

**Optional, outside the generated sign-in flow:** If you extend a server application
to require a confidential client credential (for example, to call downstream APIs),
configure a supported `AzureAd:ClientCredentials` source yourself. User secrets can
hold manually configured local development credentials, but are not an encrypted
production secret store.

Use a separate app registration for production and supply its `AzureAd` settings
through deployment tooling. If production functionality requires a credential,
configure managed identity, federated identity credentials, or a certificate as
appropriate. Do not scaffold a production registration merely to add local redirect
URIs.

### Basic Workflow

The typical process for adding Entra ID authentication to an existing project:

1. **Have an existing ASP.NET Core project** targeting .NET 10+ (created without authentication)
2. **Run the Entra ID scaffolder** to add authentication
3. **Follow the interactive prompts** (if not using command-line options)
4. **Test the authentication** in your application

#### Example 1: Command-Line Mode (All Options Specified)

```bash
# Navigate to your project directory
cd MyBlazorApp

# Run the scaffolder with all options
dotnet scaffold aspnet entra-id \
  --username john@contoso.com \
  --project ./MyBlazorApp.csproj \
  --tenantId "your-tenant-id" \
  --use-existing-application false
```

This will:
- Automatically install `dotnet msidentity` if not present
- Register a new app in your Entra ID tenant
- Generate authentication code files
- Create or update appsettings.Development.json with tenant and client information
- Add required NuGet packages
- Apply necessary code changes for authentication

No client secret is created and `UserSecretsId` is not initialized.

#### Example 2: Interactive Mode

You can also run the scaffolder with minimal options and answer interactive prompts:

```bash
cd MyBlazorApp
dotnet scaffold aspnet entra-id
```

**Illustrative interactive prompt flow** (not a verbatim console transcript):

```
? User name for the identity user:
> john@contoso.com

? .NET project to be used for scaffolding (.csproj file):
> ./MyBlazorApp.csproj

? Tenant Id for the identity user:
> 12345678-1234-1234-1234-123456789abc

? Use Existing Application? (No = Create New):
> No
  Yes
```

For a new registration, representative log messages include the following
(client ID is illustrative; logging prefixes and other messages are omitted):

```text
Registering an application for local development and testing. Use a separate app registration for production.
Captured client ID: 87654321-4321-4321-4321-210987654321
Successfully Registered App.
Updated 'appsettings.Development.json' with AzureAd development configuration.
```

If the development settings already match, the configuration step reports that no
changes are needed instead. No client secret or user-secrets store is generated.

Next steps:
1. Run `dotnet run` with the `Development` environment.
2. Navigate to a protected page.
3. Sign in with your Entra ID credentials.

#### Example 3: Interactive Mode - Selecting Existing App

```bash
cd MyBlazorApp
dotnet scaffold aspnet entra-id
```

**Illustrative interactive prompts** (not a verbatim console transcript):

```
? User name for the identity user:
> admin@contoso.com

? .NET project to be used for scaffolding (.csproj file):
> ./MyBlazorApp.csproj

? Tenant Id for the identity user:
> contoso.onmicrosoft.com

? Use Existing Application? (No = Create New):
  No
> Yes

? Select Existing Application (Application ID):
> 11111111-2222-3333-4444-555555555555
```

The explicitly selected registration is reused for the generated development
settings and local redirect URI configuration. No new registration or password
credential is created, and existing locally configured credentials are preserved.

---

## Command Usage

### Basic Command Structure

```bash
dotnet scaffold aspnet entra-id [options]
```

### Getting Help

To see all available options:

```bash
dotnet scaffold aspnet entra-id --help
```

Output:
```
Description:
  Add Entra auth

  Examples:
    Add Microsoft Entra ID authentication using an existing Azure application:
      dotnet scaffold aspnet entra-id --project C:/MyWebApp/MyWebApp.csproj --tenant-id your-tenant-id --use-existing-application true --application-id your-app-id
    Add Microsoft Entra ID authentication by creating a new Azure application:
      dotnet scaffold aspnet entra-id --project C:/MyWebApp/MyWebApp.csproj --tenant-id your-tenant-id --use-existing-application false

Usage:
  dotnet-scaffold aspnet entra-id [options]

Options:
  --username <username> (REQUIRED)               User name for the identity user
  --project <project> (REQUIRED)                 .NET project to be used for scaffolding (.csproj file)
  --tenantId <tenantId> (REQUIRED)              Tenant Id for the identity user
  --use-existing-application <true|false> (REQUIRED)  Set to true to select an existing Azure application object, or false to create a new one
  --applicationId <applicationId>                Select existing application (required when --use-existing-application is true)
  -?, -h, --help                                 Show help and usage information
```

### Required Options

The following options must be provided:

1. **--username**: Your Azure account email
2. **--project**: Path to your .csproj file
3. **--tenantId**: Your Entra ID tenant ID
4. **--use-existing-application**: Set to `true` to use existing app, or `false` to create new

### Optional Options

- **--applicationId**: Client ID of existing app (required when `--use-existing-application true`)

---

## Common Scenarios

### Scenario 1: Add Auth to a New Blazor Server Project

You created a Blazor Server app without authentication and now want to add it.

```bash
# Create project without auth
dotnet new blazorserver -n MyBlazorApp
cd MyBlazorApp

# Add Entra ID authentication
dotnet scaffold aspnet entra-id \
  --username john@contoso.com \
  --project ./MyBlazorApp.csproj \
  --tenantId "12345678-1234-1234-1234-123456789abc" \
  --use-existing-application false

# Run the app
dotnet run
```

### Scenario 2: Add Auth to Existing Blazor WebAssembly Project

Retrofit authentication into an existing Blazor WASM application.

```bash
cd MyBlazorWasmApp

dotnet scaffold aspnet entra-id \
  --username jane@contoso.com \
  --project ./MyBlazorWasmApp.csproj \
  --tenantId "contoso.onmicrosoft.com" \
  --use-existing-application false
```

### Scenario 3: Use an Existing App Registration

You already have an app registration in Azure and want to use it.

```bash
dotnet scaffold aspnet entra-id \
  --username admin@fabrikam.com \
  --project ./MyProject/MyProject.csproj \
  --tenantId "87654321-4321-4321-4321-210987654321" \
  --use-existing-application true \
  --applicationId "11111111-2222-3333-4444-555555555555"
```

### Scenario 4: Multiple Projects in Same Solution

Add authentication to multiple related projects using the same tenant.

```bash
# Project 1
dotnet scaffold aspnet entra-id \
  --username dev@company.com \
  --project ./WebApp/WebApp.csproj \
  --tenantId "your-tenant-id" \
  --use-existing-application false

# Project 2 - create separate app registration
dotnet scaffold aspnet entra-id \
  --username dev@company.com \
  --project ./AdminApp/AdminApp.csproj \
  --tenantId "your-tenant-id" \
  --use-existing-application false
```

### Scenario 5: Development → Production Workflow

Scaffold locally with a development registration. Configure a separate production
registration through deployment tooling; do not run the scaffolder against it.

```bash
# Development - create new app
dotnet scaffold aspnet entra-id \
  --username dev@contoso.com \
  --project ./MyApp.csproj \
  --tenantId "dev-tenant-id" \
  --use-existing-application false

# Production - supply AzureAd settings through deployment tooling.
# Configure credentials separately only if required by the application's functionality.
```

---

## Options Reference

### --username (REQUIRED)

The Azure AD username/email address to use for authentication.

**Format**: email@domain.com

**Example**:
```bash
--username john.doe@contoso.com
```

**Notes**:
- Must be logged in via Azure CLI with this account
- Used for registering/updating app registrations
- Helps disambiguate when multiple Azure accounts are available

---

### --project (REQUIRED)

Path to the .NET project file (.csproj) where authentication will be added.

**Format**: Absolute or relative path to .csproj file

**Examples**:
```bash
--project ./MyBlazorApp.csproj
--project C:/Projects/MyWebApp/MyWebApp.csproj
--project ../src/WebApp/WebApp.csproj
```

**Notes**:
- Project must exist and be a valid ASP.NET Core project
- Currently optimized for Blazor applications
- Project will be modified to include authentication

---

### --tenantId (REQUIRED)

The Microsoft Entra ID (Azure AD) tenant ID where the app will be registered.

**Format**: GUID (e.g., 12345678-1234-1234-1234-123456789abc) or domain name

**Examples**:
```bash
--tenantId 12345678-1234-1234-1234-123456789abc
--tenantId contoso.onmicrosoft.com
```

**How to find your tenant ID**:
1. Azure Portal > Microsoft Entra ID > Overview > Tenant ID
2. Or run: `az account show --query tenantId -o tsv`

**Notes**:
- You must have permissions to register apps in this tenant
- The tenant must be accessible with your Azure CLI login
- The tenant value, whether a GUID or domain name, is written to `AzureAd:TenantId`. The scaffolder does not generate `AzureAd:Domain`; Microsoft.Identity.Web does not require it for workforce Entra ID.
- Existing intentional `AzureAd:Domain` values are preserved, including provider-specific settings such as B2C domains. Only a malformed value matching `<current-username>.onmicrosoft.com` from the old email-based default is removed.

---

### --use-existing-application (REQUIRED)

Specifies whether to use an existing Azure AD app registration or create a new one.

**Values**:
- `true` - Select an existing Azure application object
- `false` - Create a new Azure application object

**Examples**:
```bash
# Create a new app
--use-existing-application false

# Use existing app
--use-existing-application true
```

**Notes**:
- If using existing (`true`), you must also provide --applicationId
- Creating new (`false`) is recommended for new projects

---

### --applicationId (OPTIONAL)

The client ID of an existing Azure AD app registration to use.

**Format**: GUID (e.g., 87654321-4321-4321-4321-098765432109)

**Example**:
```bash
--applicationId 87654321-4321-4321-4321-098765432109
```

**When to use**:
- Required when `--use-existing-application true`
- When you want to configure the project to use a pre-existing app registration
- When sharing an app registration across multiple projects

**How to find application ID**:
1. Azure Portal > Microsoft Entra ID > App registrations > [Your App] > Application (client) ID
2. Or run: `az ad app list --display-name "YourAppName" --query [0].appId -o tsv`

**Notes**:
- The app must exist in the specified tenant
- You must have permissions to update the app registration

---

## Common Scenarios

### Scenario 1: Add Auth to a New Blazor Server Project (Interactive Mode)

You created a Blazor Server app without authentication and now want to add it interactively.

```bash
# Create project without auth (targeting .NET 10+)
dotnet new blazorserver -n MyBlazorApp -f net10.0
cd MyBlazorApp

# Run scaffolder interactively - it will prompt for all required info
dotnet scaffold aspnet entra-id

# Follow the prompts:
# - Enter your email
# - Confirm project path
# - Enter tenant ID
# - Select "No" for "Use Existing Application?" to create a new app

# Run the app
dotnet run
```

### Scenario 2: Add Auth with Command-Line Options (Non-Interactive)

Retrofit authentication into an existing Blazor Server project using command-line options.

```bash
cd MyBlazorApp

# All options specified - no prompts
dotnet scaffold aspnet entra-id \
  --username john@contoso.com \
  --project ./MyBlazorApp.csproj \
  --tenantId "12345678-1234-1234-1234-123456789abc" \
  --use-existing-application false
```

### Scenario 3: Add Auth to Blazor WebAssembly Project (Interactive)

Retrofit authentication into an existing Blazor WASM application.

```bash
cd MyBlazorWasmApp

# Interactive mode - scaffolder will prompt for details
dotnet scaffold aspnet entra-id

# Or with all options (non-interactive):
dotnet scaffold aspnet entra-id \
  --username jane@contoso.com \
  --project ./MyBlazorWasmApp.csproj \
  --tenantId "contoso.onmicrosoft.com" \
  --use-existing-application false
```

### Scenario 4: Use an Existing App Registration (Interactive)

You already have an app registration in Azure and want to use it.

```bash
cd MyProject
dotnet scaffold aspnet entra-id

# Interactive prompts will guide you:
# ? User name: admin@fabrikam.com
# ? Project: ./MyProject.csproj
# ? Tenant ID: 87654321-4321-4321-4321-210987654321
# ? Use Existing Application?: Yes
# ? Application ID: 11111111-2222-3333-4444-555555555555
```

Or non-interactively:

```bash
dotnet scaffold aspnet entra-id \
  --username admin@fabrikam.com \
  --project ./MyProject/MyProject.csproj \
  --tenantId "87654321-4321-4321-4321-210987654321" \
  --use-existing-application true \
  --applicationId "11111111-2222-3333-4444-555555555555"
```

### Scenario 5: Multiple Projects in Same Solution

Add authentication to multiple related projects using the same tenant.

```bash
# Project 1 - interactive mode
cd WebApp
dotnet scaffold aspnet entra-id
# Answer prompts for tenant, etc.

# Project 2 - command-line mode with same tenant
cd ../AdminApp
dotnet scaffold aspnet entra-id \
  --username dev@company.com \
  --project ./AdminApp.csproj \
  --tenantId "your-tenant-id" \
  --use-existing-application false
```

### Scenario 6: Development → Production Workflow

Scaffold locally with a development registration. Configure a separate production
registration through deployment tooling; do not run the scaffolder against it.

```bash
# Development - create new app
dotnet scaffold aspnet entra-id \
  --username dev@contoso.com \
  --project ./MyApp.csproj \
  --tenantId "dev-tenant-id" \
  --use-existing-application false

# Production - supply AzureAd settings through deployment tooling.
# Configure credentials separately only if required by the application's functionality.
```

---

## What the Scaffolder Does

When you run the Entra ID scaffolder, it performs a series of automated steps to integrate Microsoft Entra ID authentication into your project.

### Step-by-Step Process

1. **Tool Check & Auto-Install**: Checks if `dotnet msidentity` is installed; automatically installs it if not present
2. **Validation**: Validates all required parameters and project file
3. **Azure CLI Authentication**: Verifies Azure CLI login status
4. **App Registration**: Calls `dotnet msidentity --register-app --registration-only` for a new registration, or reuses the explicitly selected existing registration without inferring one from base configuration
5. **Project Analysis**: Detects project type (Blazor Server/WASM, target framework)
6. **Configuration**: Creates or updates only appsettings.Development.json with sign-in settings; does not create client secrets, initialize `UserSecretsId`, or update user secrets
7. **Registration Configuration**: Configures ID token issuance and local redirect URIs
8. **Packages**: Adds required NuGet packages (Microsoft.Identity.Web, etc.)
9. **Code Modifications**: Updates Program.cs, App.razor, and other files
10. **Code Generation**: Generates authentication components from templates

### Files Created/Modified

**Created:** LoginOrLogout.razor in the applicable layout folders

**Created or modified:** appsettings.Development.json, Program.cs, App.razor, .csproj (depending on project type)

**Unchanged by this flow:** appsettings.json, user secrets; no `UserSecretsId` initialization

---

## Supported Project Types

The Entra ID Scaffolder is currently optimized for:

### Blazor Applications (Primary Support)
- **Blazor Server**: Server-side Blazor applications
- **Blazor WebAssembly**: Client-side Blazor applications
  - Standalone applications
  - Hosted applications (with Web API backend)

### Framework Support
- **.NET 10.0+**: **Required** (.NET 10 or higher)
- The Entra ID scaffolder is **only available in .NET 10 and later versions**
- Projects must be targeting .NET 10.0 or higher

### Authentication Type
- **Microsoft Entra ID** (formerly Azure AD): Single tenant authentication
- **Azure AD B2C**: Not currently supported by this scaffolder (use `dotnet msidentity` directly)

### Note
While the scaffolder is optimized for Blazor projects, it uses the underlying `dotnet msidentity` tool which supports broader project types. For non-Blazor projects (MVC, Razor Pages, Web API), consider using `dotnet msidentity` directly when creating new projects.

### Upgrading to .NET 10

If your project is targeting an earlier version of .NET:

```bash
# Check current version
dotnet --version

# Download .NET 10 SDK
# Visit: https://dotnet.microsoft.com/download/dotnet/10.0

# After installation, update your project file
# Change <TargetFramework>net8.0</TargetFramework>
# To:     <TargetFramework>net10.0</TargetFramework>
```

---

## Troubleshooting

### Common Issues

#### Issue: "entra-id scaffolder not found" or "Unknown command: entra-id"

**Solution:**
The Entra ID scaffolder is only available in .NET 10 and later:

```bash
# Check your .NET version
dotnet --version

# If less than 10.0.0, download and install .NET 10 SDK
# Visit: https://dotnet.microsoft.com/download/dotnet/10.0

# After installation, verify
dotnet --version
# Should show: 10.0.0 or higher

# Check available scaffolders
dotnet scaffold aspnet --help
```

**Note**: If you're using .NET 8 or earlier, this scaffolder won't be available. You'll need to either:
- Upgrade your project and SDK to .NET 10+, or
- Use `dotnet msidentity` directly (which supports earlier .NET versions)

---

#### Issue: "User is not authenticated" or "Azure CLI not found"

**Solution:**
Ensure Azure CLI is installed and you're logged in:
```bash
# Install Azure CLI first if not installed
# https://docs.microsoft.com/cli/azure/install-azure-cli

# Login to Azure
az login

# Verify login and see your account
az account show

# If you have multiple subscriptions, set the correct one
az account set --subscription "your-subscription-id"
```

#### Issue: "dotnet msidentity tool not found"

**Solution:**
The scaffolder should automatically install the `dotnet msidentity` tool if it's not present. However, if you encounter this error:

```bash
# The scaffolder will try to install it automatically, but if it fails:
# Install the tool manually
dotnet tool install Microsoft.dotnet-msidentity -g

# Verify installation
dotnet msidentity --version

# If already installed but not found, update it
dotnet tool update Microsoft.dotnet-msidentity -g
```

**Note**: In most cases, you don't need to manually install this tool. The scaffolder handles it automatically.

#### Issue: "Insufficient permissions to register application"

**Solution:**
- Verify you have "Application Developer" role or higher in Entra ID
- Contact your tenant administrator to grant required permissions
- Try using a different username with appropriate permissions

Check your permissions:
```bash
az ad sp list --display-name "your-username" --query "[].{DisplayName:displayName, Id:id}" -o table
```

#### Issue: "Invalid tenant  ID"

**Solution:**
Find your correct tenant ID:
```bash
# List all available tenants
az account list --query "[].{Name:name, TenantId:tenantId}" -o table

# Show current tenant
az account show --query tenantId -o tsv
```

Use the correct tenant ID from the output in your command.

#### Issue: "Project file not found"

**Solution:**
- Ensure the path to .csproj is correct
- Use absolute or relative paths carefully
- Check current directory: `pwd` (bash/PowerShell) or `cd` (cmd)

```bash
# If in project directory
--project ./MyProject.csproj

# If in parent directory
--project ./MyFolder/MyProject/MyProject.csproj

# Using absolute path
--project C:/Projects/MyApp/MyApp.csproj
```

#### Issue: "Application ID required when selecting existing app"

**Solution:**
When using `--use-existing-application true`, you must provide the `--applicationId`:

```bash
dotnet scaffold aspnet entra-id \
  --username your@email.com \
  --project ./MyApp.csproj \
  --tenantId "your-tenant-id" \
  --use-existing-application true \
  --applicationId "your-existing-app-id"
```

Find your app ID:
```bash
# List apps in your tenant
az ad app list --display-name "YourAppName" --query "[].{DisplayName:displayName, AppId:appId}" -o table
```

#### Optional manual credential setup: "Failed to add client secret"

The sign-in-only scaffolder does not create client secrets. This guidance applies
only when you separately choose to create a credential for additional application
functionality.

**Solution for manual credential creation:**
- Ensure the app registration exists
- Verify you have permission to manage the app
- Check that you're using the correct tenant ID and client ID
- Try running the msidentity command manually:
  ```bash
  dotnet msidentity --create-client-secret --client-id your-client-id --tenant-id your-tenant-id --username your@email.com
  ```

#### Issue: "Build errors after scaffolding"

**Solution:**
1. Restore packages:
   ```bash
   dotnet restore
   ```

2. Clean and rebuild:
   ```bash
   dotnet clean
   dotnet build
   ```

3. Check for package version conflicts in .csproj

4. Ensure target framework is .NET 6.0 or later

#### Optional manual credential setup: "User secrets not working"

User secrets are not initialized or populated by the sign-in-only scaffolder.
If you separately configure a local development client credential, initialize
user secrets yourself if needed:
```bash
dotnet user-secrets init --project ./MyApp.csproj

# List current secrets
dotnet user-secrets list --project ./MyApp.csproj

# Manually set secret if needed
dotnet user-secrets set "AzureAd:ClientCredentials:0:SourceType" "ClientSecret" --project ./MyApp.csproj
dotnet user-secrets set "AzureAd:ClientCredentials:0:ClientSecret" "your-development-secret" --project ./MyApp.csproj
```

### Debug Tips

1. **Run in verbose mode**: While the scaffolder doesn't have a verbose flag, you can check the underlying msidentity commands:
   ```bash
   dotnet msidentity --list-aad-apps --tenant-id your-tenant-id --username your@email.com
   ```

2. **Verify Azure login**:
   ```bash
   az account show
   az ad app list --query "[].{DisplayName:displayName, AppId:appId}" -o table
   ```

3. **Check generated files**: After scaffolding, verify:
   - appsettings.Development.json has the AzureAd section
   - appsettings.json and existing user secrets are unchanged
   - New authentication files were created

4. **Test authentication**: Run the app in the `Development` environment and try to access a protected page

---

## Best Practices

### Security

1. **Never Commit Secrets**
   - User secrets are stored outside your project directory
   - Add *.json to .gitignore if not already there
   - Never commit appsettings.json with actual secret values
   - Use Azure Key Vault or environment variables in production

2. **Rotate Manually Configured Credentials When Applicable**
   - The sign-in-only scaffolder does not create credentials
   - Rotate any client secrets you configure separately
   - Use Azure AD credential rotation features
   - Update secrets in all environments

3. **Use Separate App Registrations**
   - Development: Create separate app for local dev
   - Staging: Separate app for staging environment
   - Production: Dedicated production app registration
   - This limits blast radius if credentials are compromised

4. **Least Privilege**
   - Only grant necessary API permissions
   - Use role-based access control (RBAC)
   - Regularly audit app permissions in Azure Portal

### Development Workflow

1. **Start Clean**
   - Create projects without auth initially
   - Add authentication as a separate step using the scaffolder
   - Commit before scaffolding to easily see changes

2. **Version Control**
   ```bash
   # Before scaffolding
   git add .
   git commit -m "Before adding Entra ID auth"

   # Run scaffolder
   dotnet scaffold aspnet entra-id ...

   # Review changes
   git diff

   # Commit auth changes
   git add .
   git commit -m "Add Entra ID authentication"
   ```

3. **Test Locally First**
   - Use development app registrations for local testing
   - Test login/logout flows
   - Verify authentication works before deploying

4. **Document Your Setup**
   - Keep track of tenant IDs by environment
   - Document app registration names and IDs
   - Maintain a README with setup instructions

### Multi-Developer Teams

1. **Individual Dev App Registrations**
   - Each developer should create their own app registration
   - Use naming convention: `MyApp-Dev-<DeveloperName>`
   - Share tenant, but not app registrations or secrets

2. **Shared Development Tenant**
   - Use a dedicated development tenant if possible
   - Or create a shared "Dev" resource group
   - Document tenant access requirements

3. **Configuration Management**
   The scaffolder writes local sign-in settings only to
   `appsettings.Development.json`. It does not populate base settings or user
   secrets. The following is an **optional manual configuration example**, not
   generated output, for applications that require a confidential client credential:

   ```json
   // appsettings.json - shared, committed
   {
     "AzureAd": {
       "Instance": "https://login.microsoftonline.com/"
     }
   }

   // secrets.json - local, not committed
   {
     "AzureAd": {
       "ClientCredentials": [
         {
           "SourceType": "ClientSecret",
           "ClientSecret": "your-dev-secret"
         }
       ]
     }
   }
   ```

4. **CI/CD Considerations**
   - Don't run the scaffolder in CI/CD pipelines
   - Scaffold locally and commit the generated code
   - Supply production settings and any required credentials through deployment tooling
   - Consider Azure Managed Identities for Azure-hosted apps

### Project Organization

1. **Keep Authentication Code Separate**
   - The scaffolder generates auth files in appropriate folders
   - Don't mix authentication logic with business logic
   - Use the generated login/logout component

2. **Environment-Specific Configuration**
   - appsettings.Development.json: Generated local sign-in settings; loaded in `Development`
   - appsettings.Production.json: Optional manually maintained production settings, not generated by this flow
   - User secrets: Optional manually configured local development credentials, not generated by this flow
   - Environment variables: For non-local environments

3. **Blazor-Specific**
   - Use `<AuthorizeView>` components for conditional UI
   - Apply `[Authorize]` attributes to pages that require auth
   - Test both authenticated and anonymous user experiences

### Maintenance

1. **Update Tools Regularly**
   ```bash
   # Update .NET SDK
   # https://dotnet.microsoft.com/download

   # Update dotnet msidentity tool
   dotnet tool update Microsoft.dotnet-msidentity -g

   # Update NuGet packages
   dotnet list package --outdated
   dotnet add package Microsoft.Identity.Web
   ```

2. **Monitor App Registrations**
   - Review app registrations in Azure Portal periodically
   - Remove unused or test app registrations
   - If you configure credentials separately, monitor their configured lifetimes and rotate them before expiry

3. **Stay Current**
   - Follow Microsoft Identity platform updates
   - Review .NET authentication security advisories
   - Test auth flows after major framework updates

---

## Additional Resources

### Documentation
- **Microsoft Entra ID**: https://learn.microsoft.com/entra/identity/
- **Microsoft Identity Platform**: https://learn.microsoft.com/azure/active-directory/develop/
- **ASP.NET Core Authentication**: https://learn.microsoft.com/aspnet/core/security/authentication/
- **Blazor Authentication**: https://learn.microsoft.com/aspnet/core/blazor/security/
- **Azure CLI**: https://learn.microsoft.com/cli/azure/

### Tools
- **dotnet msidentity**: The underlying tool used by the scaffolder
  - NuGet: https://www.nuget.org/packages/Microsoft.dotnet-msidentity
  - Source: Part of this repository
- **Azure CLI**: https://learn.microsoft.com/cli/azure/install-azure-cli
- **Microsoft Graph Explorer**: https://developer.microsoft.com/graph/graph-explorer

### Related Projects
- **Microsoft.Identity.Web**: NuGet package for ASP.NET Core authentication
- **MSAL.NET**: Microsoft Authentication Library for .NET
- **Blazor Authentication Samples**: https://github.com/dotnet/blazor-samples

---

## Summary

The **Microsoft Entra ID Scaffolder** (`dotnet scaffold aspnet entra-id`) is a powerful tool for adding Microsoft Entra ID authentication to existing ASP.NET Core applications, particularly Blazor projects.

### Key Takeaways

✅ **For Existing Projects**: Adds auth to projects created without it
✅ **Automated Sign-In Setup**: Handles app registration, code generation, and Development-only configuration without creating client secrets or initializing user secrets

✅ **Azure CLI Based**: Uses your existing Azure login
✅ **Auto-Installs Dependencies**: Automatically installs `dotnet msidentity` tool if needed
✅ **.NET 10+ Only**: Requires .NET 10.0 or later
✅ **Interactive or Command-Line**: Run with prompts or specify all options
✅ **Blazor Optimized**: Best experience with Blazor Server and Blazor WebAssembly
✅ **Separate Production Configuration**: Supply production settings and any required credentials through deployment tooling

### Quick Start Reminder

```bash
# 1. Prerequisites
az login
# Note: dotnet msidentity will be auto-installed by the scaffolder if needed

# 2. Add authentication to your Blazor project (.NET 10+ required)
dotnet scaffold aspnet entra-id \
  --username your@email.com \
  --project ./YourApp.csproj \
  --tenantId "your-tenant-id" \
  --use-existing-application false

# Or run interactively:
dotnet scaffold aspnet entra-id

# 3. Run and test with the Development environment
dotnet run
```

### When to Use This Tool

**Use `dotnet scaffold aspnet entra-id` when:**
- ✅ Adding auth to an existing Blazor project (.NET 10+)
- ✅ You want automated, guided setup with optional interactive prompts
- ✅ Working with Blazor Server or Blazor WebAssembly
- ✅ Your project targets .NET 10.0 or later

**Use `dotnet msidentity` directly when:**
- ✅ Creating new projects with `dotnet new --auth SingleOrg`
- ✅ Working with MVC, Razor Pages, or Web API projects
- ✅ Need more control over the authentication setup
- ✅ Working with Azure AD B2C
- ✅ Your project targets .NET 8 or earlier (the entra-id scaffolder is not available)

For questions or issues, refer to the troubleshooting section or check the official Microsoft documentation.

---

**Last Updated**: February 2026
**Scaffolder**: `dotnet scaffold aspnet entra-id`
**Minimum .NET Version**: .NET 10.0 (scaffolder only available in .NET 10+)
**Underlying Tool**: `dotnet msidentity` (auto-installed if needed)
**Supported Frameworks**: .NET 10.0+
**Primary Project Types**: Blazor Server, Blazor WebAssembly
