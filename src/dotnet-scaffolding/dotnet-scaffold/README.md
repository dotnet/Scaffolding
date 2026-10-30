New and improved scaffolding experience. 
More details coming soon!

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
