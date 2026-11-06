// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;
using System.Threading.Tasks;
using Microsoft.DotNet.Tools.Scaffold.Tests.Helpers;
using Xunit;

namespace Microsoft.DotNet.Tools.Scaffold.Tests.AspNet.Integration.Blazor;

[Trait("Suite", "ScaffoldIntegration")]
[Trait("Family", "blazor-crud")]
public class BlazorCrudVsInputsTests
{
    [Fact]
    public async Task Scaffold_Net10IndividualAccounts_NewProjectNamedContext_ResolvesFactory()
    {
        using var project = new BlazorTestProject("net10.0");
        var create = await ScaffoldCliHelper.RunDotNetAsync(project.ProjectDirectory,
            "new", "blazor", "--name", "TestProject", "--output", ".", "--framework", "net10.0",
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
        var contextName = Path.GetFileNameWithoutExtension(project.ProjectPath) + "Context";
        Assert.False(File.Exists(Path.Combine(project.ProjectDirectory, contextName + ".cs")));
        var before = await ScaffoldCliHelper.RunBuildAsync(project.ProjectDirectory);
        Assert.True(before.ExitCode == 0, $"Input build failed.\n{before.Output}\n{before.Error}");

        var scaffold = await ScaffoldCliHelper.RunScaffoldAsync(
            ScaffoldCliHelper.GetTestTargetFramework(), "blazor-crud",
            "--project", project.ProjectPath, "--model", "Movie", "--dataContext", contextName,
            "--dbProvider", "sqlserver-efcore", "--page", "CRUD");
        Assert.True(scaffold.ExitCode == 0, $"Scaffolding failed.\n{scaffold.Output}\n{scaffold.Error}");

        var programPath = Path.Combine(project.ProjectDirectory, "Program.cs");
        var program = File.ReadAllText(programPath);
        Assert.Contains("app.Run();", program);
        File.WriteAllText(programPath, program.Replace("app.Run();", $$"""
            using var scope = app.Services.CreateScope();
            var identity = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var factory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<{{contextName}}>>();
            using var movies = factory.CreateDbContext();
            void VerifyConnection(DbContext context, string connectionName)
            {
                var actual = new Microsoft.Data.SqlClient.SqlConnectionStringBuilder(context.Database.GetConnectionString());
                var expected = new Microsoft.Data.SqlClient.SqlConnectionStringBuilder(builder.Configuration.GetConnectionString(connectionName));
                actual.Remove("Application Name");
                expected.Remove("Application Name");
                if (!actual.EquivalentTo(expected))
                {
                    throw new InvalidOperationException($"The connection for {connectionName} was changed.");
                }
            }
            VerifyConnection(identity, "DefaultConnection");
            VerifyConnection(movies, "{{contextName}}");
            Console.WriteLine("Project-named SQL Server context factory and Identity connection verified.");
            """));
        var build = await ScaffoldCliHelper.RunBuildAsync(project.ProjectDirectory);
        Assert.True(build.ExitCode == 0, $"Generated build failed.\n{build.Output}\n{build.Error}");
        var verify = await ScaffoldCliHelper.RunDotNetAsync(project.ProjectDirectory,
            "run", "--no-build", "--no-launch-profile");
        Assert.True(verify.ExitCode == 0, $"Factory resolution failed.\n{verify.Output}\n{verify.Error}");
        Assert.Contains("Project-named SQL Server context factory and Identity connection verified.", verify.Output);
        Assert.Contains("AddDbContext<ApplicationDbContext>", program);
        Assert.Contains($"GetConnectionString(\"{contextName}\")", program);
        var index = File.ReadAllText(Path.Combine(project.ProjectDirectory, "Components", "Pages", "MoviePages", "Index.razor"));
        Assert.Contains("@page \"/movies\"", index);
        Assert.DoesNotContain("@page \"/moviepages\"", index);
        Assert.Contains($"@inject IDbContextFactory<{contextName}> DbFactory", index);
    }
}
