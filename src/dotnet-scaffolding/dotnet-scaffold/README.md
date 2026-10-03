New and improved scaffolding experience. 
More details coming soon!

## Identity for MVC and Razor Pages

Add local ASP.NET Core Identity to an MVC or Razor Pages project:

```powershell
dotnet scaffold aspnet identity --project .\MyApp\MyApp.csproj --dataContext ApplicationDbContext --dbProvider sqlite-efcore
```

Use `--prerelease` when targeting a preview of .NET. The scaffolder adds Identity pages, host configuration, login navigation, and an initial EF Core migration. It does not apply the migration or update the database.

Existing Identity registrations, user types, and login partials are preserved. Running the same command again without `--overwrite` leaves the generated source unchanged. For customized layouts without a recognizable navbar, the scaffolder generates `_LoginPartial.cshtml` and reports where to add the reference manually.