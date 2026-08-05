# Known Flaky and Skipped Integration Tests

This document tracks integration tests that have historically been **flaky** (intermittently
failing without a code change) or are currently **skipped**. It exists so contributors can tell
the difference between a real regression they introduced and a pre-existing, known-unstable test.

> **Before you debug a failing integration test:** check whether it appears below. If a test here
> fails intermittently, re-run it before assuming your change caused the failure (see
> [Dealing with a flaky failure](#dealing-with-a-flaky-failure)). If a test here is listed as
> **skipped**, it is expected to be skipped — do not treat it as a failure.

Most of these live under
`test/dotnet-scaffolding/dotnet-scaffold.Tests/` in the `AspNet/Integration/` and
`Aspire/Integration/` folders. Each scaffolder has a shared `...IntegrationTestsBase.cs` and one
concrete class per target framework (`...Net8`, `...Net9`, `...Net10`, `...Net11`).

---

## Table of Contents
- [Why these tests are prone to flakiness](#why-these-tests-are-prone-to-flakiness)
- [Historically flaky: net11 / preview-SDK scaffolding tests](#historically-flaky-net11--preview-sdk-scaffolding-tests)
- [Currently skipped integration tests](#currently-skipped-integration-tests)
- [Dealing with a flaky failure](#dealing-with-a-flaky-failure)
- [Reporting a newly flaky test](#reporting-a-newly-flaky-test)

---

## Why these tests are prone to flakiness

The scaffolding integration tests are **end-to-end**: they create a real project, run the
scaffolder, then restore and build the generated code. That makes them sensitive to factors
outside the test's own logic:

- **NuGet package/version resolution.** Tests restore packages from live feeds. Transient
  metadata-query failures or version resolution that reads the wrong `NuGet.config`/feeds have
  produced intermittent `NU1202` / framework-incompatibility errors and failed restores.
- **Preview SDK availability.** `net11.0` (and other preview) targets depend on preview SDKs and
  preview feeds being installed and correctly configured on the machine. Missing or mismatched
  preview SDKs cause intermittent build failures or silently no-op scaffolding steps.
- **External services / credentials.** The Entra ID scaffolder shells out to `dotnet msidentity`,
  which validates tenant IDs against Azure AD and needs real credentials.
- **Network and CI environment.** Feed latency, throttling, and CI image differences can all
  surface as intermittent failures.

---

## Historically flaky: net11 / preview-SDK scaffolding tests

These tests have failed intermittently in CI due to **NuGet package-version resolution** and
**preview SDK** issues. The underlying causes have been addressed in the following fixes, but the
`net11`/preview-SDK area remains the most likely source of flakiness, so treat failures here with
extra suspicion before blaming your change:

| Area | Symptom | Related fix |
|------|---------|-------------|
| net11 scaffolding restore/build | `null`/incorrect preview package versions → flaky restore/build because version resolution read the scaffold process CWD instead of the target project's feeds | "Fix flaky net11 scaffolding tests: resolve package versions from project's NuGet feeds" (#3778) |
| CI NuGet package resolution | Version resolution silently returned `null` on transient NuGet metadata query failures, causing `dotnet add package` to install a framework-incompatible package → flaky `NU1202` | "Stabilize flaky NuGet package resolution in CI" (#3783) |
| Debug build on CI | `includePreviewVersions` during .NET 10 SDK install caused debug build flakiness | "Fix CI: remove includePreviewVersions from .NET 10 SDK install" |

**Affected test families** (the `Net11` variant is the most affected; base classes are shared with
the other frameworks):

- `AspNet/Integration/API/` — `ApiControllerNet11IntegrationTests`, `MinimalApiNet11IntegrationTests`
- `AspNet/Integration/Blazor/` — `BlazorCrudNet11IntegrationTests`, `RazorComponentNet11IntegrationTests`
- `AspNet/Integration/Identity/` — `IdentityNet11IntegrationTests`, `BlazorIdentityNet11IntegrationTests`
- `AspNet/Integration/MVC/` — `ControllerNet11IntegrationTests`, `CrudControllerNet11IntegrationTests`, `AreaNet11IntegrationTests`, `RazorViewsNet11IntegrationTests`, `RazorViewEmptyNet11IntegrationTests`
- `AspNet/Integration/RazorPages/` — `RazorPagesCrudNet11IntegrationTests`, `RazorPageEmptyNet11IntegrationTests`

---

## Currently skipped integration tests

These integration tests are skipped on purpose. A build/test run that shows them as **skipped** is
expected — do **not** count them as failures, and do not remove the `Skip` reason without fixing
the underlying issue.

### ASP.NET (`AspNet/Integration/`)

| Test | Skip reason |
|------|-------------|
| `Blazor/BlazorCrudNet11IntegrationTests.Scaffold_BlazorCrud_Net11_CliInvocation` | net11.0 preview SDK not yet supported |
| `API/ApiControllerNet11IntegrationTests.Scaffold_ApiControllerCrud_Net11_CliInvocation` | net11.0 preview SDK not yet supported |
| `Identity/IdentityNet11IntegrationTests.Scaffold_Identity_Net11_CliInvocation` | net11.0 preview SDK not yet supported |
| `RazorPages/RazorPagesCrudNet11IntegrationTests.Scaffold_RazorPagesCrud_Net11_CliInvocation` | net11.0 preview SDK silently no-ops the `Program.cs` code-modification step; re-enable once root cause is fixed |
| `MVC/RazorViewsNet11IntegrationTests.Scaffold_Views_Net11_CliInvocation` | net11.0 preview SDK silently no-ops the views templating step; re-enable once root cause is fixed |
| `EntraId/EntraIdNet10IntegrationTests.Scaffold_EntraId_Net10_CliInvocation` | Requires real Azure AD credentials — `entra-id` scaffolder calls `dotnet msidentity`, which validates the tenant-id against Azure AD |
| `EntraId/EntraIdNet11IntegrationTests.Scaffold_EntraId_Net11_CliInvocation` | Requires real Azure AD credentials — `entra-id` scaffolder calls `dotnet msidentity`, which validates the tenant-id against Azure AD |

### Aspire (`Aspire/Integration/`)

All test methods in the following base classes are skipped with the reason
**"Aspire tests on separate branch"** (they run against Aspire caching, database, and storage
scaffolders):

- `AspireCachingIntegrationTestsBase`
- `AspireDatabaseIntegrationTestsBase`
- `AspireStorageIntegrationTestsBase`

### MSIdentity (`test/MSIdentityScaffolding/`)

| Test | Skip reason |
|------|-------------|
| `ProjectDescriptionReaderTests.TestProjectDescriptionReader` | Test gets stuck on macOS and Linux. Tracking [dotnet/Scaffolding#1598](https://github.com/dotnet/Scaffolding/issues/1598) |
| `ProjectDescriptionReaderTests.TestProjectDescriptionReader_TemplatesWithBlazorWasmHosted` | Test gets stuck on macOS and Linux. Tracking [dotnet/Scaffolding#1598](https://github.com/dotnet/Scaffolding/issues/1598) |
| `ProjectDescriptionReaderTests.TestProjectDescriptionReader_TemplatesWithBlazorWasm` | The newly created test project wants packages that don't exist on official feeds; will rework for next update |
| `ProjectDescriptionReaderTests.TestProjectDescriptionReader_TemplatesWithNoAuth` | The newly created test project wants packages that don't exist on official feeds; will rework for next update |

> This list reflects the skip reasons present in the source at the time of writing. The `Skip`
> attribute string on each test method is always the source of truth — search the test files for
> `Skip =` to see the current set.

---

## Dealing with a flaky failure

If an integration test in this document fails during your work:

1. **Re-run it in isolation** to see if it's transient:
   ```bash
   dotnet test test/dotnet-scaffolding/dotnet-scaffold.Tests/dotnet-scaffold.Tests.csproj \
     --filter "FullyQualifiedName~BlazorCrudNet11"
   ```
2. **Confirm it's unrelated to your change** by re-running the same test on a clean checkout of
   `main`. If it also fails there, it's a pre-existing flake, not a regression you caused.
3. **Rule out environment issues** for preview-SDK/NuGet flakiness:
   - Ensure the preview SDK matching `global.json` is installed.
   - Clear caches if results look stale:
     ```bash
     dotnet nuget locals all --clear
     ```
     and delete stray `.config` folders in the repo and any test projects.
4. **Do not silently `Skip` a test** to make CI green. If a test must be disabled, add a `Skip`
   reason that links to a tracking issue, and note it here.

---

## Reporting a newly flaky test

If you find an integration test that fails intermittently and isn't listed here:

1. Open an issue with the test's fully qualified name, the failure output, how often it fails, and
   the environment (OS, SDK version, CI vs. local).
2. If it blocks CI, skip it with a descriptive reason that links the tracking issue:
   ```csharp
   [Fact(Skip = "Flaky: <short description>. Tracking https://github.com/dotnet/Scaffolding/issues/<n>")]
   ```
3. Add it to the [Currently skipped integration tests](#currently-skipped-integration-tests) table
   so others know it's expected.

See [CONTRIBUTING.md](../CONTRIBUTING.md) for the full testing and pull-request workflow.
