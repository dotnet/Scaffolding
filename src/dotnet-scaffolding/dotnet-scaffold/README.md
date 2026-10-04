New and improved scaffolding experience. 
More details coming soon!

## Identity scaffolding

Add local ASP.NET Core Identity to an MVC or Razor Pages project:

```powershell
dotnet scaffold aspnet identity --project .\MyApp\MyApp.csproj --dataContext ApplicationDbContext --dbProvider sqlite-efcore
```

For a Blazor app, use `aspnet blazor-identity` with the same project, data context, and database provider options. Use `--prerelease` when targeting a preview of .NET.

Both scaffolders configure Identity UI, services, the DbContext, and the connection string. Neither creates migrations or modifies the database. `Microsoft.EntityFrameworkCore.Design` is included to support your manual EF CLI workflow; use a compatible `dotnet-ef` tool.

After scaffolding, review the complete EF model and manage schema changes using your application's normal migration and database deployment workflow. If a migration is needed, choose a suitable name, for example:

```powershell
dotnet ef migrations add AddIdentity --project .\MyApp\MyApp.csproj --context ApplicationDbContext
```

An existing migration snapshot does not necessarily mean that no further migration is needed.

Running the same command again without `--overwrite` leaves the generated source unchanged. For MVC and Razor Pages, existing Identity registrations, user types, and login partials are preserved. For customized layouts without a recognizable navbar, the scaffolder generates `_LoginPartial.cshtml` and reports where to add the reference manually.