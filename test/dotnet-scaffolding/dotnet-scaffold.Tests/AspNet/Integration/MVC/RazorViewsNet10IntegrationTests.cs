// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.DotNet.Tools.Scaffold.Tests.Helpers;
using Xunit;

namespace Microsoft.DotNet.Tools.Scaffold.Tests.AspNet.Integration.MVC;

public class RazorViewsNet10IntegrationTests : RazorViewsIntegrationTestsBase
{
    protected override string TargetFramework => "net10.0";
    protected override string TestClassName => nameof(RazorViewsNet10IntegrationTests);

    [Fact]
    public async Task Scaffold_Views_Net10_CreateWithKeylessModel()
    {
        File.WriteAllText(_testProjectPath, ProjectContent);
        var viewsDir = Directory.CreateDirectory(Path.Combine(_testProjectDir, "Views")).FullName;
        File.WriteAllText(Path.Combine(viewsDir, "_ViewImports.cshtml"), "@addTagHelper *, Microsoft.AspNetCore.Mvc.TagHelpers");
        File.WriteAllText(Path.Combine(_testProjectDir, "Program.cs"), """
            using Microsoft.AspNetCore.Mvc;

            var builder = WebApplication.CreateBuilder(args);
            builder.Services.AddControllersWithViews();
            await using var app = builder.Build();
            app.MapControllerRoute("default", "{controller}/{action}");
            app.Urls.Add("http://127.0.0.1:0");
            using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(30));
            await app.StartAsync(timeout.Token);
            using var client = new HttpClient(new HttpClientHandler { UseProxy = false });
            Console.WriteLine(await client.GetStringAsync(app.Urls.Single() + "/LoginViewModel/Create", timeout.Token));
            await app.StopAsync(timeout.Token);

            public class LoginViewModelController : Controller
            {
                public IActionResult Create() => View(new TestProject.LoginViewModel());
            }
            """);
        File.WriteAllText(Path.Combine(_testProjectDir, "LoginViewModel.cs"), """
            namespace TestProject;

            public class LoginViewModelBase
            {
                public virtual string Email { get; set; }
                public string Password { get; set; }
                public virtual string DisplayName { get; set; }
                public string PhoneNumber { get; set; }
            }

            public class LoginViewModel : LoginViewModelBase
            {
                public override string Email { get; set; }
                public new string Password { get; set; }
                public override string DisplayName => base.DisplayName;
                private new string PhoneNumber { get; set; }
                public bool RememberMe { get; set; }
                private string PrivateProperty { get; set; }
                public static string StaticProperty { get; set; }
                public string this[int index] => "";
                public string ReadOnlyProperty => "";
            }
            """);

        var (preExitCode, preOutput, preError) = await RunBuildAsync(_testProjectDir);
        Assert.True(preExitCode == 0,
            $"Project should build before scaffolding.\nOutput: {preOutput}\nError: {preError}");

        var (cliExitCode, cliOutput, cliError) = await ScaffoldCliHelper.RunScaffoldAsync(
            TargetFramework,
            "views",
            "--project", _testProjectPath,
            "--model", "LoginViewModel",
            "--page", "Create");
        Assert.True(cliExitCode == 0, $"CLI scaffold should succeed.\nOutput: {cliOutput}\nError: {cliError}");

        var (runExitCode, html, runError) = await ScaffoldCliHelper.RunDotNetAsync(_testProjectDir, "run", "--no-launch-profile");
        Assert.True(runExitCode == 0, $"The generated MVC view should render.\nOutput: {html}\nError: {runError}");
        Assert.Single(Regex.Matches(html, "<input[^>]*name=\"Email\""));
        Assert.Single(Regex.Matches(html, "<input[^>]*name=\"Password\""));
        Assert.Single(Regex.Matches(html, "<input[^>]*name=\"DisplayName\""));
        Assert.Single(Regex.Matches(html, "<input[^>]*name=\"PhoneNumber\""));
        Assert.Matches("<input[^>]*type=\"checkbox\"[^>]*name=\"RememberMe\"", html);
        Assert.DoesNotContain("PrivateProperty", html);
        Assert.DoesNotContain("StaticProperty", html);
        Assert.DoesNotContain("ReadOnlyProperty", html);
    }

    [Fact]
    public async Task Scaffold_Views_Net10_CliInvocation()
    {
        File.WriteAllText(_testProjectPath, ProjectContent);
        File.WriteAllText(Path.Combine(_testProjectDir, "Program.cs"), ScaffoldCliHelper.GetMinimalProgramCs());
        var modelsDir = Path.Combine(_testProjectDir, "Models");
        Directory.CreateDirectory(modelsDir);
        File.WriteAllText(Path.Combine(modelsDir, "TestModel.cs"), ScaffoldCliHelper.GetModelClassContent("TestProject", "TestModel"));

        var (preExitCode, preOutput, preError) = await RunBuildAsync(_testProjectDir);
        Assert.True(preExitCode == 0,
            $"Project should build before scaffolding.\nExit code: {preExitCode}\nOutput: {preOutput}\nError: {preError}");

        var (cliExitCode, cliOutput, cliError) = await ScaffoldCliHelper.RunScaffoldAsync(
            TargetFramework,
            "views",
            "--project", _testProjectPath,
            "--model", "TestModel",
            "--page", "CRUD");
        Assert.True(cliExitCode == 0, $"CLI scaffold should succeed.\nOutput: {cliOutput}\nError: {cliError}");

        // Assert — expected files were created
        var viewsDir = Path.Combine(_testProjectDir, "Views", "TestModel");
        Assert.True(Directory.Exists(viewsDir), "Views/TestModel directory should be created.");
        foreach (var view in new[] { "Create.cshtml", "Delete.cshtml", "Details.cshtml", "Edit.cshtml", "Index.cshtml" })
        {
            Assert.True(File.Exists(Path.Combine(viewsDir, view)), $"View '{view}' should be created.");
        }
        Assert.True(File.Exists(Path.Combine(_testProjectDir, "Views", "Shared", "_ValidationScriptsPartial.cshtml")),
            "_ValidationScriptsPartial.cshtml should be created.");

        // Assert — no NuGet errors and project builds after scaffolding
        Assert.False(cliOutput.Contains("error: NU"),
            $"Scaffolding should not produce NuGet errors for {TargetFramework}.\nOutput: {cliOutput}");
        var (postExitCode, postOutput, postError) = await RunBuildAsync(_testProjectDir);
        Assert.True(postExitCode == 0,
            $"Project should build after scaffolding.\nExit code: {postExitCode}\nOutput: {postOutput}\nError: {postError}");
    }
}
