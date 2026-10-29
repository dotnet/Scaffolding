// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Xml.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Text;
using Microsoft.DotNet.Scaffolding.Shared;
using Microsoft.DotNet.Scaffolding.Shared.CodeModifier.CodeChange;
using Microsoft.DotNet.Scaffolding.Shared.Project;
using Microsoft.DotNet.Scaffolding.Shared.ProjectModel;
using Microsoft.DotNet.Scaffolding.Shared.T4Templating;
using Microsoft.VisualStudio.Web.CodeGeneration;
using Microsoft.VisualStudio.Web.CodeGeneration.DotNet;
using Microsoft.VisualStudio.Web.CodeGeneration.EntityFrameworkCore;
using Microsoft.VisualStudio.Web.CodeGenerators.Mvc.Blazor;
using Microsoft.VisualStudio.Web.CodeGenerators.Mvc.BlazorIdentity;
using Moq;
using Xunit;

namespace Microsoft.VisualStudio.Web.CodeGenerators.Mvc
{
    public class BlazorIdentityGeneratorTests : IDisposable
    {
        private readonly string _root = Path.Combine(Path.GetTempPath(), $"identity-generator-{Guid.NewGuid():N}");
        private readonly AdhocWorkspace _workspace = new AdhocWorkspace();
        private readonly BlazorIdentityModel _model = new BlazorIdentityModel
        {
            BlazorIdentityNamespace = "Server.Components.Account",
            DbContextNamespace = "Server.Data",
            DbContextName = "ApplicationDbContext",
            UserClassName = "ApplicationUser",
            DatabaseProvider = DbProvider.SQLite
        };

        [Theory]
        [InlineData("InteractiveAuto", true)]
        [InlineData("InteractiveWebAssembly", true)]
        [InlineData("InteractiveAuto", false)]
        public async Task ModifyFiles_UpdatesBothProjectsWithoutServerDependenciesInClient(string renderMode, bool global)
        {
            var client = AddProject("Client", "Microsoft.NET.Sdk.BlazorWebAssembly",
                "using Microsoft.AspNetCore.Components.WebAssembly.Hosting;\nvar clientBuilder = WebAssemblyHostBuilder.CreateDefault(args);\nawait clientBuilder.Build().RunAsync();");
            var server = AddProject("Server", "Microsoft.NET.Sdk.Web",
                "var builder = WebApplication.CreateBuilder(args);\nbuilder.Services.AddRazorComponents().AddInteractiveWebAssemblyComponents();\nvar app = builder.Build();\napp.Run();");
            Assert.True(_workspace.TryApplyChanges(_workspace.CurrentSolution.AddProjectReference(server.Id, new ProjectReference(client.Id))));
            var uiProject = global ? "Client" : "Server";
            var components = global ? "" : "Components";
            Write(uiProject, Path.Combine(components, "Routes.razor"),
                "<Router AppAssembly=\"typeof(Program).Assembly\"><Found Context=\"routeData\"><RouteView RouteData=\"routeData\" DefaultLayout=\"typeof(Layout.MainLayout)\" /></Found></Router>");
            Write(uiProject, Path.Combine(components, "Layout", "NavMenu.razor"),
                "<div class=\"top-row ps-3 navbar navbar-dark\">\n</div>\n<nav>\n        <div class=\"nav-item px-3\">\n            <NavLink class=\"nav-link\" href=\"weather\">\n                <span class=\"bi bi-list-nested-nav-menu\" aria-hidden=\"true\"></span> Weather\n            </NavLink>\n        </div>\n</nav>");
            Write(uiProject, Path.Combine(components, "Layout", "NavMenu.razor.css"), ".nav-item {\n}");
            Write("Server", Path.Combine("Components", "_Imports.razor"), "@using Microsoft.AspNetCore.Components.Forms");
            Write("Client", "_Imports.razor", "@using Microsoft.AspNetCore.Components.Forms");
            Write("Server", Path.Combine("Components", "App.razor"), global
                ? $"<HeadOutlet @rendermode=\"{renderMode}\" /><Routes @rendermode=\"{renderMode}\" /></body>"
                : "<HeadOutlet /><Routes /></body>");

            var generator = CreateGenerator();
            await generator.ModifyFilesAsync(_model);

            var serverProgram = Read("Server", "Program.cs");
            var clientProgram = Read("Client", "Program.cs");
            Assert.Contains("AddAuthenticationStateSerialization()", serverProgram);
            Assert.DoesNotContain("AddAuthorizationCore()", serverProgram);
            Assert.Contains("AddAuthorizationCore()", clientProgram);
            Assert.Contains("AddCascadingAuthenticationState()", clientProgram);
            Assert.Contains("AddAuthenticationStateDeserialization()", clientProgram);
            Assert.DoesNotContain("Server.", clientProgram);
            Assert.DoesNotContain("Identity", clientProgram);
            Assert.Contains("clientBuilder.Services.AddAuthorizationCore()", clientProgram);
            Assert.Contains("AuthorizeRouteView", Read(uiProject, Path.Combine(components, "Routes.razor")));
            Assert.DoesNotContain("Server.Components.Account", Read(uiProject, Path.Combine(components, "Routes.razor")));
            var nav = Read(uiProject, Path.Combine(components, "Layout", "NavMenu.razor"));
            Assert.Contains("Account/Register", nav);
            Assert.Contains("Account/Login", nav);
            Assert.Contains("AuthorizeView", nav);
            Assert.Contains("bi-lock-nav-menu", Read(uiProject, Path.Combine(components, "Layout", "NavMenu.razor.css")));
            var imports = Read("Client", "_Imports.razor");
            Assert.Contains("Microsoft.AspNetCore.Components.Authorization", imports);
            Assert.DoesNotContain("Microsoft.AspNetCore.Components.Endpoints", imports);
            Assert.Contains("Microsoft.AspNetCore.Components.Endpoints", Read("Server", Path.Combine("Components", "_Imports.razor")));
            Assert.Contains("forceLoad: true", Read("Client", "RedirectToLogin.razor"));
            Assert.False(Directory.Exists(Path.Combine(_root, "Client", "Components", "Account")));

            await generator.ModifyFilesAsync(_model);
            Assert.Equal(serverProgram, Read("Server", "Program.cs"));
            Assert.Equal(clientProgram, Read("Client", "Program.cs"));
        }

        [Fact]
        public async Task ModifyProgram_AllowsMissingUsingsAndSelectsTargetProject()
        {
            AddProject("Server", "Microsoft.NET.Sdk.Web", "var builder = WebApplication.CreateBuilder(args);\nvar app = builder.Build();\napp.Run();");
            var client = AddProject("Client", "Microsoft.NET.Sdk.BlazorWebAssembly", "var builder = WebAssemblyHostBuilder.CreateDefault(args);\nawait builder.Build().RunAsync();");
            var originalServer = Read("Server", "Program.cs");
            var changes = new CodeFile { FileName = "Program.cs" };

            await CreateGenerator().ModifyProgramCsAsync(changes, _model, client);

            Assert.Equal(originalServer, Read("Server", "Program.cs"));
            Assert.DoesNotContain("Server.Data", Read("Client", "Program.cs"));
        }

        [SkippableTheory]
        [InlineData("Auto")]
        [InlineData("WebAssembly")]
        public async Task StockGlobalTemplate_ScaffoldsAndBuildsBothProjects(string interactivity)
        {
            Skip.If(string.IsNullOrEmpty(Environment.GetEnvironmentVariable("SCAFFOLDING_RunSkippableTests")));
            Directory.CreateDirectory(_root);
            await RunDotnet("new", "blazor", "--name", "Server", "--output", _root,
                "--framework", "net10.0", "--interactivity", interactivity, "--all-interactive", "--no-restore");
            var client = AddExistingProject("Server.Client");
            var server = AddExistingProject("Server");
            Assert.True(_workspace.TryApplyChanges(_workspace.CurrentSolution.AddProjectReference(server.Id, new ProjectReference(client.Id))));
            var clientXml = XDocument.Load(Path.Combine(_root, "Server.Client", "Server.Client.csproj"));
            var version = (string)clientXml.Descendants("PackageReference").Single().Attribute("Version");
            var serverPath = Path.Combine(_root, "Server", "Server.csproj");
            var serverXml = XDocument.Load(serverPath);
            serverXml.Root.Add(new XElement("ItemGroup",
                new XElement("PackageReference", new XAttribute("Include", "Microsoft.AspNetCore.Identity.EntityFrameworkCore"), new XAttribute("Version", version)),
                new XElement("PackageReference", new XAttribute("Include", "Microsoft.EntityFrameworkCore.Sqlite"), new XAttribute("Version", version))));
            serverXml.Save(serverPath);
            var context = new Mock<IProjectContext>();
            context.SetupGet(c => c.AssemblyName).Returns("Server");
            context.SetupGet(c => c.RootNamespace).Returns("Server");
            context.SetupGet(c => c.PackageDependencies).Returns(new[]
            {
                new DependencyDescription("Microsoft.VisualStudio.Web.CodeGenerators.Mvc", "1.0", AppContext.BaseDirectory,
                    "net10.0", DependencyType.Package, true)
            });
            var app = new Mock<IApplicationInfo>();
            app.SetupGet(a => a.ApplicationBasePath).Returns(Path.Combine(_root, "Server"));
            var generator = new BlazorIdentityGenerator(app.Object, new ModelTypesLocator(_workspace),
                Mock.Of<ILogger>(), DefaultFileSystem.Instance, Mock.Of<IFilesLocator>(), context.Object,
                Mock.Of<IEntityFrameworkService>(), Mock.Of<ICodeGenAssemblyLoadContext>(), _workspace);
            _model.BaseOutputPath = Path.Combine(_root, "Server");
            _model.BlazorLayoutNamespace = "Server.Client.Layout.MainLayout";
            _model.UserClassName = "IdentityUser";
            _model.UserClassNamespace = "Microsoft.AspNetCore.Identity";
            _model.FilesToGenerate = Directory.GetFiles(Path.Combine(AppContext.BaseDirectory, "Templates", "BlazorIdentity"),
                "*.tt", SearchOption.AllDirectories).Select(BlazorIdentityHelper.GetFormattedRelativeIdentityFile).ToList();
            Assert.NotEmpty(_model.FilesToGenerate);
            var dbContext = new Templates.General.IdentityDbContext { Session = new Dictionary<string, object>() };
            var dbContextText = new TemplateInvoker().InvokeTemplate(dbContext, new Dictionary<string, object>
            {
                ["Model"] = new Templates.General.IdentityDbContextModel
                {
                    DbContextName = _model.DbContextName,
                    DbContextNamespace = _model.DbContextNamespace,
                    UserClassModel = new Templates.General.IdentityApplicationUserModel
                    {
                        UserClassName = _model.UserClassName,
                        UserClassNamespace = _model.UserClassNamespace
                    }
                }
            });
            Write("Server", Path.Combine("Data", "ApplicationDbContext.cs"), dbContextText);
            generator.ExecuteTemplates(_model);
            generator.AddStaticFiles(_model);
            await generator.ModifyFilesAsync(_model);

            var clientRoutes = Read("Server.Client", "Routes.razor");
            Assert.Contains("AuthorizeRouteView", clientRoutes);
            Assert.DoesNotContain("Server.Components.Account", clientRoutes);
            Assert.Contains("Account/Register", Read("Server.Client", Path.Combine("Layout", "NavMenu.razor")));
            Assert.Contains("Account/Login", Read("Server.Client", Path.Combine("Layout", "NavMenu.razor")));
            Assert.True(File.Exists(Path.Combine(_root, "Server", "Components", "Account", "Pages", "Login.razor")));
            Assert.True(File.Exists(Path.Combine(_root, "Server", "Components", "Account", "Pages", "Register.razor")));
            Assert.False(Directory.Exists(Path.Combine(_root, "Server.Client", "Components", "Account")));
            Assert.Contains("AddAuthenticationStateSerialization()", Read("Server", "Program.cs"));
            Assert.Contains("AddAuthenticationStateDeserialization()", Read("Server.Client", "Program.cs"));
            Assert.Contains($"? Interactive{interactivity} : null", Read("Server", Path.Combine("Components", "App.razor")));
            Assert.Contains("[ExcludeFromInteractiveRouting]", Read("Server", Path.Combine("Components", "Account", "Pages", "_Imports.razor")));
            await RunDotnet("build", serverPath, "-p:UseSharedCompilation=false", "--nologo");
        }

        private Project AddExistingProject(string name)
        {
            var project = _workspace.AddProject(ProjectInfo.Create(ProjectId.CreateNewId(), VersionStamp.Create(),
                name, name, LanguageNames.CSharp, filePath: Path.Combine(_root, name, $"{name}.csproj"),
                compilationOptions: new CSharpCompilationOptions(OutputKind.ConsoleApplication)));
            var path = Path.Combine(_root, name, "Program.cs");
            Assert.True(_workspace.TryApplyChanges(_workspace.CurrentSolution.AddDocument(DocumentId.CreateNewId(project.Id),
                "Program.cs", SourceText.From(File.ReadAllText(path)), filePath: path)));
            return _workspace.CurrentSolution.GetProject(project.Id);
        }

        private async Task RunDotnet(params string[] arguments)
        {
            var startInfo = new ProcessStartInfo("dotnet")
            {
                WorkingDirectory = _root,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false
            };
            foreach (var argument in arguments)
            {
                startInfo.ArgumentList.Add(argument);
            }
            using var process = Process.Start(startInfo);
            var output = process.StandardOutput.ReadToEndAsync();
            var error = process.StandardError.ReadToEndAsync();
            try
            {
                await process.WaitForExitAsync().WaitAsync(TimeSpan.FromMinutes(5));
            }
            catch (TimeoutException)
            {
                process.Kill(entireProcessTree: true);
                throw;
            }
            Assert.True(process.ExitCode == 0, $"dotnet {string.Join(" ", arguments)} failed:\n{await output}\n{await error}");
        }

        private Project AddProject(string name, string sdk, string program)
        {
            var packages = sdk == "Microsoft.NET.Sdk.BlazorWebAssembly"
                ? "<ItemGroup><PackageReference Include=\"Microsoft.AspNetCore.Components.WebAssembly\" Version=\"10.0.14\" /></ItemGroup>"
                : "";
            var path = Write(name, $"{name}.csproj", $"<Project Sdk=\"{sdk}\">{packages}</Project>");
            var project = _workspace.AddProject(ProjectInfo.Create(ProjectId.CreateNewId(), VersionStamp.Create(),
                name, name, LanguageNames.CSharp, filePath: path,
                compilationOptions: new CSharpCompilationOptions(OutputKind.ConsoleApplication)));
            var programPath = Write(name, "Program.cs", program);
            Assert.True(_workspace.TryApplyChanges(_workspace.CurrentSolution.AddDocument(DocumentId.CreateNewId(project.Id),
                "Program.cs", SourceText.From(program), filePath: programPath)));
            return _workspace.CurrentSolution.GetProject(project.Id);
        }

        private BlazorIdentityGenerator CreateGenerator()
        {
            var context = new Mock<IProjectContext>();
            context.SetupGet(c => c.AssemblyName).Returns("Server");
            return new BlazorIdentityGenerator(Mock.Of<IApplicationInfo>(), new ModelTypesLocator(_workspace),
                Mock.Of<ILogger>(), DefaultFileSystem.Instance, Mock.Of<IFilesLocator>(), context.Object,
                Mock.Of<IEntityFrameworkService>(), Mock.Of<ICodeGenAssemblyLoadContext>(), _workspace);
        }

        private string Write(string project, string relativePath, string content)
        {
            var path = Path.Combine(_root, project, relativePath);
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            File.WriteAllText(path, content);
            return path;
        }

        private string Read(string project, string path) => File.ReadAllText(Path.Combine(_root, project, path));

        public void Dispose()
        {
            _workspace.Dispose();
            if (Directory.Exists(_root))
            {
                Directory.Delete(_root, recursive: true);
            }
        }
    }
}
