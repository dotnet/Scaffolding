// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using Microsoft.DotNet.Tools.Scaffold.Tests.Helpers;
using Xunit;

namespace Microsoft.DotNet.Tools.Scaffold.Tests.AspNet.Integration.Blazor;

[Trait("Suite", "ScaffoldIntegration")]
[Trait("Family", "blazor-crud")]
public class BlazorCrudIdentityRegressionTests
{
    [Theory]
    [InlineData("net9.0", "new")]
    [InlineData("net10.0", "new")]
    [InlineData("net10.0", "spaced")]
    [InlineData("net10.0", "custom")]
    [InlineData("net10.0", "literal")]
    [InlineData("net10.0", "qualified")]
    [InlineData("net10.0", "alias")]
    [InlineData("net10.0", "scoped")]
    public async Task Scaffold_WithIndividualAccounts_RegistersSelectedContextFactory(string framework, string registration)
        => await ScaffoldAndVerifyAsync(framework, registration);

    [Fact]
    public async Task Scaffold_ConditionalContextRegistration_FailsWithActionableDiagnostic()
        => await ScaffoldAndVerifyAsync("net10.0", "conditional");

    [SkippableTheory]
    [Trait("Requires", "SqlServerLocalDb")]
    [InlineData("net9.0")]
    [InlineData("net10.0")]
    public async Task Scaffold_WithIndividualAccounts_RendersMoviesFromSqlServer(string framework)
    {
        Skip.IfNot(OperatingSystem.IsWindows(), "SQL Server LocalDB requires Windows.");
        await ScaffoldAndVerifyAsync(framework, "new", verifyPage: true);
    }

    private static async Task ScaffoldAndVerifyAsync(string framework, string registration, bool verifyPage = false)
    {
        using var project = new BlazorTestProject(framework);
        var create = await ScaffoldCliHelper.RunDotNetAsync(project.ProjectDirectory,
            "new", "blazor", "--name", "TestProject", "--output", ".", "--framework", framework,
            "--auth", "Individual", "--use-local-db", "--interactivity", "Server", "--force", "--no-restore");
        Assert.True(create.ExitCode == 0, $"Template creation failed.\n{create.Output}\n{create.Error}");
        File.WriteAllText(Path.Combine(project.ProjectDirectory, "Movie.cs"), """
            namespace TestProject;

            public class Movie
            {
                public int ID { get; set; }
                public string Title { get; set; } = string.Empty;
                public DateTime ReleaseDate { get; set; }
                public string Genre { get; set; } = string.Empty;
                public decimal Price { get; set; }
            }
            """);
        var programPath = Path.Combine(project.ProjectDirectory, "Program.cs");
        if (registration is "spaced" or "custom" or "qualified" or "alias" or "scoped" or "conditional")
        {
            File.WriteAllText(Path.Combine(project.ProjectDirectory, "MovieContext.cs"), """
                using Microsoft.EntityFrameworkCore;

                public class MovieContext(DbContextOptions<MovieContext> options) : DbContext(options)
                {
                    public DbSet<TestProject.Movie> Movie { get; set; } = default!;
                }

                public class CustomMovieFactory(DbContextOptions<MovieContext> options) : IDbContextFactory<MovieContext>
                {
                    public MovieContext CreateDbContext() => new(options);
                }

                public class ScopedMovieOptions(Microsoft.Extensions.Configuration.IConfiguration configuration)
                {
                    public string? ConnectionString => configuration.GetConnectionString("MovieContext");
                }
                """);
        }
        var existingRegistration = registration switch
        {
            "spaced" => """builder.Services.AddDbContext< MovieContext >(options => options.UseSqlServer(builder.Configuration.GetConnectionString("MovieContext")));""",
            "custom" => """builder.Services.AddDbContextFactory<MovieContext, CustomMovieFactory>(options => options.UseSqlServer(builder.Configuration.GetConnectionString("MovieContext")));""",
            "literal" => """Console.WriteLine("builder.Services.AddDbContextFactory<MovieContext>");""",
            "qualified" => """builder.Services.AddDbContext<global::MovieContext>(options => options.UseSqlServer(builder.Configuration.GetConnectionString("MovieContext")));""",
            "alias" => """builder.Services.AddDbContext<SelectedContext>(options => options.UseSqlServer(builder.Configuration.GetConnectionString("MovieContext")));""",
            "scoped" => """
                builder.Services.AddScoped<ScopedMovieOptions>();
                builder.Services.AddDbContext<MovieContext>((services, options) =>
                    options.UseSqlServer(services.GetRequiredService<ScopedMovieOptions>().ConnectionString),
                    optionsLifetime: ServiceLifetime.Scoped);
                """,
            "conditional" => """if (args.Length != 0) { builder.Services.AddDbContext<MovieContext>(); }""",
            _ => ""
        };
        File.WriteAllText(programPath, File.ReadAllText(programPath).Replace("var app = builder.Build();",
            existingRegistration + """

            builder.Host.UseDefaultServiceProvider(options =>
            {
                options.ValidateScopes = true;
                options.ValidateOnBuild = true;
            });
            var app = builder.Build();
            """));
        if (registration == "alias")
        {
            File.WriteAllText(programPath, "using SelectedContext = MovieContext;\n" + File.ReadAllText(programPath));
        }
        var before = await ScaffoldCliHelper.RunBuildAsync(project.ProjectDirectory);
        Assert.True(before.ExitCode == 0, $"Input project build failed.\n{before.Output}\n{before.Error}");

        var scaffold = await ScaffoldCliHelper.RunScaffoldAsync(
            ScaffoldCliHelper.GetTestTargetFramework(), "blazor-crud",
            "--project", project.ProjectPath, "--model", "Movie", "--dataContext", "MovieContext",
            "--dbProvider", "sqlserver-efcore", "--page", "CRUD");
        if (registration == "conditional")
        {
            Assert.NotEqual(0, scaffold.ExitCode);
            Assert.Contains("unconditional startup statement", scaffold.Output + scaffold.Error);
            return;
        }
        Assert.True(scaffold.ExitCode == 0, $"Scaffolding failed.\n{scaffold.Output}\n{scaffold.Error}");

        var program = File.ReadAllText(programPath);
        Assert.Contains("AddDbContextFactory<", program);
        Assert.Contains("GetConnectionString(\"MovieContext\")", program);
        Assert.Contains("GetConnectionString(\"DefaultConnection\")", program);
        Assert.Contains("AddEntityFrameworkStores<ApplicationDbContext>", program);
        Assert.Contains("AddDbContext<ApplicationDbContext>", program);
        var index = File.ReadAllText(Path.Combine(project.ProjectDirectory, "Components", "Pages", "MoviePages", "Index.razor"));
        Assert.Contains("@page \"/movies\"", index);
        Assert.Contains("@inject IDbContextFactory<MovieContext> DbFactory", index);

        File.WriteAllText(programPath, program.Replace("app.Run();", """
            if (args.Contains("--verify-factories") || args.Contains("--verify-page"))
            {
                using var scope = app.Services.CreateScope();
                var identity = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                var factory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<MovieContext>>();
                if (builder.Services.Count(service => service.ServiceType == typeof(IDbContextFactory<MovieContext>)) != 1)
                {
                    throw new InvalidOperationException("The selected context must have exactly one factory registration.");
                }
                if (args.Contains("--expect-custom") && factory.GetType().Name != "CustomMovieFactory")
                {
                    throw new InvalidOperationException("The existing custom factory was replaced.");
                }
                using var movies = factory.CreateDbContext();
                var movieConnection = new Microsoft.Data.SqlClient.SqlConnectionStringBuilder(movies.Database.GetConnectionString());
                var expectedMovieConnection = new Microsoft.Data.SqlClient.SqlConnectionStringBuilder(builder.Configuration.GetConnectionString("MovieContext"));
                // EF Core 10 adds an application name for SQL Server diagnostics.
                movieConnection.Remove("Application Name");
                expectedMovieConnection.Remove("Application Name");
                if (!movieConnection.EquivalentTo(expectedMovieConnection))
                {
                    throw new InvalidOperationException("The movie factory uses the wrong connection string.");
                }
                var identityConnection = new Microsoft.Data.SqlClient.SqlConnectionStringBuilder(identity.Database.GetConnectionString());
                var expectedIdentityConnection = new Microsoft.Data.SqlClient.SqlConnectionStringBuilder(builder.Configuration.GetConnectionString("DefaultConnection"));
                identityConnection.Remove("Application Name");
                expectedIdentityConnection.Remove("Application Name");
                if (!identityConnection.EquivalentTo(expectedIdentityConnection))
                {
                    throw new InvalidOperationException("The Identity connection string was changed.");
                }
                if (args.Contains("--verify-page"))
                {
                    if (!movieConnection.InitialCatalog.StartsWith("ScaffoldFactoryRegression_", StringComparison.Ordinal))
                    {
                        throw new InvalidOperationException("Page verification requires an isolated test database.");
                    }
                    try
                    {
                        await movies.Database.EnsureCreatedAsync();
                        var movie = new TestProject.Movie
                        {
                            Title = "Factory regression movie",
                            ReleaseDate = new DateTime(2026, 1, 1),
                            Genre = "Regression",
                            Price = 12.50m
                        };
                        movies.Movie.Add(movie);
                        await movies.SaveChangesAsync();
                        app.Urls.Clear();
                        app.Urls.Add("http://127.0.0.1:0");
                        await app.StartAsync();
                        try
                        {
                            using var client = new System.Net.Http.HttpClient
                            {
                                BaseAddress = new Uri(app.Urls.Single()),
                                Timeout = TimeSpan.FromSeconds(30)
                            };
                            var html = await client.GetStringAsync("/movies");
                            if (!html.Contains(movie.Title) || !html.Contains("Create New"))
                            {
                                throw new InvalidOperationException("The movies index did not render the SQL Server movie.");
                            }
                            foreach (var route in new[] { "details", "edit", "delete" })
                            {
                                var page = await client.GetStringAsync($"/movies/{route}?id={movie.ID}");
                                if (!page.Contains(movie.Title))
                                {
                                    throw new InvalidOperationException($"The generated {route} page did not render the movie.");
                                }
                            }
                            await client.GetStringAsync("/movies/create");
                            async Task SubmitForm(string route, Dictionary<string, string> values)
                            {
                                var form = await client.GetStringAsync(route);
                                foreach (System.Text.RegularExpressions.Match input in
                                    System.Text.RegularExpressions.Regex.Matches(form, "<input\\b[^>]*>"))
                                {
                                    if (!input.Value.Contains("type=\"hidden\""))
                                    {
                                        continue;
                                    }
                                    var name = System.Text.RegularExpressions.Regex.Match(input.Value, "name=\"([^\"]*)\"").Groups[1].Value;
                                    var value = System.Text.RegularExpressions.Regex.Match(input.Value, "value=\"([^\"]*)\"").Groups[1].Value;
                                    if (!string.IsNullOrEmpty(name))
                                    {
                                        values.TryAdd(System.Net.WebUtility.HtmlDecode(name), System.Net.WebUtility.HtmlDecode(value));
                                    }
                                }
                                using var response = await client.PostAsync(route, new System.Net.Http.FormUrlEncodedContent(values));
                                response.EnsureSuccessStatusCode();
                            }
                            var fields = new Dictionary<string, string>
                            {
                                ["Movie.Title"] = "Created through generated form",
                                ["Movie.ReleaseDate"] = "2026-01-02",
                                ["Movie.Genre"] = "Regression",
                                ["Movie.Price"] = "15.25"
                            };
                            await SubmitForm("/movies/create", fields);
                            var created = await movies.Movie.AsNoTracking().SingleAsync(item => item.Title == fields["Movie.Title"]);
                            fields["Movie.ID"] = created.ID.ToString(System.Globalization.CultureInfo.InvariantCulture);
                            fields["Movie.Title"] = "Edited through generated form";
                            fields.Remove("_handler");
                            fields.Remove("__RequestVerificationToken");
                            await SubmitForm($"/movies/edit?id={created.ID}", fields);
                            var edited = await movies.Movie.AsNoTracking().SingleAsync(item => item.ID == created.ID);
                            if (edited.Title != fields["Movie.Title"])
                            {
                                throw new InvalidOperationException("The generated edit submission did not update the database.");
                            }
                            await SubmitForm($"/movies/delete?id={created.ID}", new Dictionary<string, string>());
                            if (await movies.Movie.AsNoTracking().AnyAsync(item => item.ID == created.ID))
                            {
                                throw new InvalidOperationException("The generated delete submission did not delete the movie.");
                            }
                            Console.WriteLine("Generated create, edit and delete submissions verified against SQL Server.");
                            Console.WriteLine("Generated CRUD pages rendered successfully against SQL Server.");
                        }
                        finally
                        {
                            await app.StopAsync();
                        }
                    }
                    finally
                    {
                        await movies.Database.EnsureDeletedAsync();
                    }
                }
                Console.WriteLine("Both contexts resolved with their own connection strings.");
                return;
            }
            app.Run();
            """));
        if (verifyPage)
        {
            var settingsPath = Path.Combine(project.ProjectDirectory, "appsettings.json");
            var settings = JsonNode.Parse(File.ReadAllText(settingsPath))!;
            settings["ConnectionStrings"]!["MovieContext"] =
                $@"Server=(localdb)\MSSQLLocalDB;Database=ScaffoldFactoryRegression_{Guid.NewGuid():N};Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True";
            File.WriteAllText(settingsPath, settings.ToJsonString());
        }
        var after = await ScaffoldCliHelper.RunBuildAsync(project.ProjectDirectory);
        Assert.True(after.ExitCode == 0, $"Generated project build failed.\n{after.Output}\n{after.Error}");
        var verify = await ScaffoldCliHelper.RunDotNetAsync(project.ProjectDirectory,
            "run", "--no-build", "--no-launch-profile", "--", verifyPage ? "--verify-page" : "--verify-factories",
            registration == "custom" ? "--expect-custom" : "--expect-default");
        Assert.True(verify.ExitCode == 0, $"Factory resolution failed.\n{verify.Output}\n{verify.Error}");
        Assert.Contains("Both contexts resolved with their own connection strings.", verify.Output);
        if (verifyPage)
        {
            Assert.Contains("Generated CRUD pages rendered successfully against SQL Server.", verify.Output);
            Assert.Contains("Generated create, edit and delete submissions verified against SQL Server.", verify.Output);
        }
    }
}
