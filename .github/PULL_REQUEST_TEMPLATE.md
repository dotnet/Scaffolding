
- [ ] The code builds and tests pass (verified by our automated build checks)
- [ ] Commit messages follow this format
        Summary of the changes
        - Detail 1
        - Detail 2

## Summary

This PR updates the Blazor Identity scaffolder to automatically apply the
required host-level integration changes when Identity is scaffolded into a
Blazor Web App.

Previously, Identity pages were generated successfully, but the application
was not fully configured to support them, resulting in runtime failures when
navigating to Identity endpoints.

## Problem

The Identity scaffolder generated the Account components but did not apply
the additional host wiring required for those components to function in a
Blazor Web App.

As a result:

- Identity pages could be generated successfully.
- Required authentication services were not fully configured.
- Identity routes were not properly integrated.
- Navigating to Identity pages could result in runtime failures.

## Changes

### `Program.cs`

Added the required Identity-related application configuration:

- Added `AddCascadingAuthenticationState()`
- Added required Identity service registrations
- Added support for additional Identity assemblies
- Added `MapAdditionalIdentityEndpoints()`

### `App.razor`

Updated the generated configuration to support Identity pages that require
non-interactive routing.

### Additional Identity Integration

Integrated the host-level changes directly into the scaffolding workflow so
that newly scaffolded applications are configured automatically without
requiring manual updates.

## Integration Tests

This PR adds/updates integration tests to verify that host wiring and
generated files are created as expected. Key test expectations include:

- The generated `Program.cs` contains the required Identity service
  registrations.
- `MapAdditionalIdentityEndpoints()` is present in the generated host file.
- `IdentityRedirectManager`, `IdentityUserAccessor`, and the authentication
  state provider are registered.
- The generated `App.razor` contains the expected non-interactive routing
  configuration for Identity pages.
- `ApplicationUser.cs`, the requested DbContext, and Identity account pages
  are created.
- The generated project builds successfully after scaffolding.

The .NET 8 coverage is provided by
`BlazorIdentityNet8IntegrationTests.Scaffold_BlazorIdentity_Net8_CliInvocation`.
The test now performs these checks unconditionally instead of passing when
the scaffolder produces no Identity files.

## Validation

Validated by:

1. Creating Blazor Web App projects without Identity.
2. Running the Identity scaffolder.
3. Verifying the generated host configuration files and `App.razor`.
4. Verifying the expected Identity files are created.
5. Building the generated project after scaffolding.
6. Running the updated integration tests for .NET 8, .NET 9, and .NET 10.

## Compatibility

- No public API changes.
- No breaking changes.
- Changes only affect the generated output of the Blazor Identity
      scaffolder.
- The host-wiring changes support .NET 8, .NET 9, and .NET 10 Blazor Web
      Apps.

## .NET 8 compatibility note

.NET 8 Blazor Identity scaffolding already has dedicated templates and a
code-modification configuration in this repository. This PR extends the new
host wiring to that configuration and fixes the .NET 8 template selection so
the expected Identity files are generated.

.NET 8 does not provide `HttpContext.AcceptsInteractiveRouting()` or
`ExcludeFromInteractiveRouting`. Its generated `App.razor` therefore uses a
framework-compatible path check to disable interactive rendering for
`/Account` routes while retaining interactive server rendering elsewhere.

This behavior is validated by the .NET 8 integration test, which confirms
the generated files, `Program.cs` registrations and endpoint mapping,
`App.razor` routing changes, and a successful post-scaffolding build.

## Tests

- Integration tests added/updated as described above.
- Unit tests added where applicable to validate the JSON-driven
      CodeModification configs used to insert host wiring.

## Related

- Fixes: #2694


