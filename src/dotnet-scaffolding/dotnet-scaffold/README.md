New and improved scaffolding experience. 
More details coming soon!

### Empty MVC Razor views

For .NET 8, 9, 10 and 11 projects, specify the destination relative to the
project directory, not the current working directory:

```powershell
dotnet scaffold aspnet razorview-empty --project ".\MyMvcApp\MyMvcApp.csproj" --name Index --relativeFolderPath "Views\Orders"
```

This creates `Views\Orders\Index.cshtml` inside `MyMvcApp`, creating the
destination directories as needed. Use the controller name without the
`Controller` suffix. Destinations such as `Views\Shared` and
`Areas\Admin\Views\Orders` are also supported. Both slash styles are accepted;
absolute paths and paths outside the project directory are rejected.
Existing views are not overwritten.

Without `--relativeFolderPath`, the existing `Views`-root default is preserved.
The tool cannot infer a controller from a view name alone. IDE integrations must
pass the destination for controller-action Add View and use the selected folder
for project-level Add View.

This command creates an empty view using `dotnet new view`; it does not require
`Microsoft.VisualStudio.Web.CodeGeneration.Design`. It does not expose
model-based templates, custom layout selection or script-library options.
Use `razorview` below for those backend capabilities. Visual Studio still needs
to wire its dialog and command invocation to the richer command.

### Single Razor view with template and layout options

The `razorview` command supports .NET 8, 9, 10 and 11 without installing a
code-generation design package into the project:

```powershell
dotnet scaffold aspnet razorview --project ".\MyMvcApp.csproj" --name NewProduct --template Create --model MyMvcApp.Models.Product --relativeFolderPath "Views\Products" --useDefaultLayout --referenceScriptLibraries
```

| Option | Behavior |
| --- | --- |
| `--name` | Required view file name; the `.cshtml` extension is optional. |
| `--template` | `Empty` (default), `Create`, `Edit`, `Delete`, `Details` or `List`. |
| `--model` | Required for non-empty templates; use a simple or fully qualified model class name. Ambiguous names are rejected. |
| `--relativeFolderPath` | Destination relative to the project directory; defaults to `Views`. Pass the controller-specific path explicitly. |
| `--useDefaultLayout` | Inherit the layout from `_ViewStart.cshtml`. |
| `--layout` | Custom layout name or application-relative path; cannot be combined with `--useDefaultLayout`. |
| `--referenceScriptLibraries` | Add a validation-partial reference and copy `_ValidationScriptsPartial.cshtml` to `Views\Shared` if missing. Existing partials are preserved. |

Without either layout option, the view explicitly sets `Layout = null`.
With a layout, the validation partial is rendered in a `Scripts` section; the
layout must render that section. Without a layout, the partial is rendered inline.
The partial references validation libraries; it does not install client-side
JavaScript assets. The MVC project's existing assets must provide those files.
Non-empty templates reuse the existing versioned CRUD view templates and require
a model with public properties and a recognized primary key. `List` uses the
index/list template, but writes the file name supplied by `--name`.
Existing views are rejected rather than overwritten.

All options, model-picker metadata and the supported template choices are
published by `dotnet scaffold get-commands`. This enables consumers to discover
the richer backend. It does not update Visual Studio's fixed Add Razor View
dialog, add conditional controls to that dialog, or infer controller context.
The existing `razorview-empty` and `views` commands retain their behavior.

### Entra ID account selection

Entra ID scaffolding currently uses both `dotnet msidentity` and the Azure CLI.
Before creating or updating an application, scaffolding verifies that the Azure
CLI's active tenant and user match the selected `--tenantId` and `--username`.
Service principal logins cannot be used in place of the selected user.
Tenant GUIDs and tenant domains are supported. For a domain selection, the CLI
reads the issuer from Microsoft's HTTPS OpenID Connect discovery endpoint and
compares its canonical tenant GUID with the active account. Discovery failure
fails validation; it does not fall back to accepting an unverified tenant.

If validation fails, run `az login --tenant <tenant-id> --allow-no-subscriptions`,
sign in as the selected user, and verify the active identity with
`az account show` before retrying. Scaffolding does not switch the CLI account
automatically.

Authorization is configured before creating a development client secret or
updating app settings. If authorization, secret configuration, or app-settings
persistence fails, scaffolding attempts to delete only the application created
during that run, including its
password credentials. Existing applications are never deleted. Cleanup failures
identify the application and tenant requiring manual removal.

Cleanup uses `dotnet msidentity` with the selected tenant and username, not the
ambient Azure CLI account. Deleting the application also deletes its service
principal in its home tenant, as described in
[Microsoft Entra application removal documentation](https://learn.microsoft.com/en-us/entra/identity-platform/howto-remove-app).
Service principals in other tenants are outside this cleanup scope.
No new password credential is created when using an existing application.

The CLI identity is checked again immediately before authorization. These checks
do not lock the global CLI session: another process could still change it between
validation and the update. Avoid changing the CLI account during scaffolding.
This is the issue's CLI-validation fallback, not a retained Graph credential.
It validates the Azure CLI account, not the effective account returned by
MSIdentity's separate interactive authentication. When prompted by MSIdentity,
sign in as the selected user; its current authentication implementation does not
enforce the returned username. Preflight therefore does not guarantee the
effective identity of every operation.

Project detection runs before registration so a detection failure cannot leave
a newly created application or credential.
New applications are created with MSIdentity's `--create-app-registration`
command, which does not write project configuration after creation.
Cleanup with an explicit client ID, tenant, and username bypasses project
configuration inference, so damaged local settings cannot block deletion.

MSIdentity also attempts to delete a partially created registration using its
returned object ID and the same Graph client if a subsequent registration
operation fails. This requires the updated MSIdentity tool, not just an updated
scaffolder. Before creating an application, scaffolding checks the capabilities
of the tool actually invoked by `dotnet msidentity`. It must report
`registration-cleanup` through `--get-capabilities`; older tools (including ones
that merely print help for an unknown command) are rejected before creation.
Install an updated build containing this fix, updating the global tool or local
tool manifest as applicable. A version number alone is not used because older
development builds can share the same release version.
If deletion fails, its diagnostics identify the resource for manual
removal; no deletion targets are discovered by display name.

Once the new application's identifier has been recorded, cleanup is best-effort
and covers authorization, credential setup, and app-settings persistence, not
subsequent package installation or code generation failures. If registration
fails before Graph returns an identifier (for example, an ambiguous network
failure during creation), inspect the selected tenant manually.
Settings persistence prepares both settings files before writing. If a write
is needed, only scaffold-managed identity properties are merged into development
settings; development-only credentials and unrelated properties are preserved.
Both settings files match property names case-insensitively and accept comments
and trailing commas, consistent with
ASP.NET Core JSON configuration. Rewriting serializes JSON without preserving
comments or original formatting.
If a write
fails, it attempts to restore the original contents of every attempted settings
write, removing a newly created settings file when necessary. Restoration errors
identify files requiring manual recovery. This is not full-run rollback:
previous project-file edits and saved user secrets are not restored or removed.
After a failed run, check local configuration before retrying.
