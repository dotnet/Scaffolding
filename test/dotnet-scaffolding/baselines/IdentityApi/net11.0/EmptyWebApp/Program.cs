using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
var builder = WebApplication.CreateBuilder(args);
var identityConnectionString = builder.Configuration.GetConnectionString("ApplicationDbContext") ?? throw new InvalidOperationException("Connection string 'ApplicationDbContext' not found.");

builder.Services.AddDbContext<global::ApplicationDbContext>(options => options.UseSqlite(identityConnectionString));

builder.Services.AddIdentityApiEndpoints<global::EmptyWebApp.Data.ApplicationUser>().AddEntityFrameworkStores<global::ApplicationDbContext>();

builder.Services.AddAuthorization();
var app = builder.Build();

app.MapGet("/", () => "Hello World!");

app.MapGroup("/identity").MapScaffoldedIdentityApi<global::EmptyWebApp.Data.ApplicationUser>();

app.Run();
