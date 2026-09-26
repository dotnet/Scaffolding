# Blazor Identity baselines

These are runnable examples of adding Identity to the default .NET 10 and .NET 11 Blazor Web App: per-page Blazor Server interactivity, SQLite, and an `ApplicationDbContext`. They are expected output, not another implementation of the templates.

Each `BlazorIdentity\<framework>\BaselineApp` is an ordinary application. The surrounding build files isolate it from the repository's Arcade and central package management settings. The adjacent `global.json` selects an SDK for that framework; it can roll forward within that .NET version.

## Run an application

From the chosen `BaselineApp` directory:

```powershell
dotnet run -- --environment Development
```

Use the address printed by the application. Launch profiles are omitted to avoid random ports in the baselines. The home, login, and registration pages work without database setup. To exercise account creation and login, use a matching version of the `dotnet-ef` tool:

```powershell
dotnet ef migrations add InitialCreate
dotnet ef database update
dotnet run -- --environment Development
```

Migrations are not scaffolder output; create them in a disposable copy if you want to keep the baseline directory clean. Database files and build outputs are excluded from comparison. These are development examples with the scaffolded no-op email sender, not production authentication configurations.

## Check the output

Build the CLI and run the baseline tests from the repository root using the repository's normal SDK setup:

```powershell
dotnet build src\dotnet-scaffolding\dotnet-scaffold\dotnet-scaffold.csproj
dotnet test test\dotnet-scaffolding\dotnet-scaffold.Tests\dotnet-scaffold.Tests.csproj --filter FullyQualifiedName~BlazorIdentityBaselineTests
```

The tests use the existing CLI process helpers and the normal `ScaffoldIntegration` / `blazor-identity` CI selection. They run the .NET 11 CLI against both application frameworks; the existing integration tests retain downlevel CLI-runtime coverage.

For each application the test:

1. Restores and builds a temporary copy of the expected application.
2. Installs the exact web-template package pinned in `dotnet-scaffold.Tests.csproj` into a private template hive. It does not change your installed templates.
3. Runs `dotnet new blazor --name BaselineApp --framework <framework> --no-restore --exclude-launch-settings`.
4. Invokes `aspnet blazor-identity --project <project> --dataContext ApplicationDbContext --dbProvider sqlite-efcore`, adding `--prerelease` for .NET 11, and builds the generated application.
5. Compares the result and reports missing/unexpected files and the first differing line in each changed file. Failed runs retain their temporary applications; the test output reports the path.

`scaffolded-files.txt` lists the files Identity is expected to add or modify. Their complete contents must match the runnable baseline. All other input files must remain unchanged, so an unrelated template asset update does not require updating its frozen baseline copy. New or deleted files are also checked. Only line endings are normalized; package versions, markup, namespaces, and whitespace remain significant.

The baseline project's exact package versions are the dependency pins. After restoring the expected application into an isolated package directory, the test offers only that dependency closure through a temporary local NuGet source, without live feeds. This keeps version selection deterministic without changing the scaffolder or masking package-reference differences. A new dependency must first be added to the expected application; otherwise scaffolding fails or the project-file comparison catches it. SDK and network availability still affect the initial restore.

## Change the expected behavior

Prefer editing and running the expected application first, reviewing that diff, and then updating the scaffolder until the test matches. Update `scaffolded-files.txt` when intentionally adding or modifying another file. Keep its paths relative, one per line, with `/` separators. Do not add variants merely to cover another combination of options; retain focused regression tests for those differences.

For an intentional refresh from actual scaffolder output:

```powershell
$env:UPDATE_BLAZOR_IDENTITY_BASELINES = '1'
try {
    dotnet test test\dotnet-scaffolding\dotnet-scaffold.Tests\dotnet-scaffold.Tests.csproj --filter FullyQualifiedName~BlazorIdentityBaselineTests
}
finally {
    Remove-Item Env:\UPDATE_BLAZOR_IDENTITY_BASELINES
}
```

This builds before replacing the baseline source and regenerating its changed-file list. It is an explicit acceptance operation, not a fix for an unexplained failure. Review the entire diff and rerun without the variable. To change dependencies, edit the baseline project's exact versions first. To change template inputs, update the pinned template package in the test project deliberately.

The baselines replace common generated-content assertions in the .NET 10/11 Identity tests. They do not replace custom-project, failure-path, overwrite, global-interactivity, provider-specific, or browser behavior tests. Building and matching expected code is not proof that authentication flows work.
