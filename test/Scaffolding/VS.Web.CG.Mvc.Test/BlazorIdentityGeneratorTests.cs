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
using Microsoft.VisualStudio.Web.CodeGeneration.Utils;
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
        private RoslynWorkspace _productionWorkspace;
        private readonly BlazorIdentityModel _model = new BlazorIdentityModel
        {
            BlazorIdentityNamespace = "Server.Components.Account",
            DbContextNamespace = "Server.Data",
            DbContextName = "ApplicationDbContext",
            UserClassName = "ApplicationUser",
            DatabaseProvider = DbProvider.SQLite
        };

        [Theory]
        [InlineData("InteractiveAuto", true, "Attribute")]
        [InlineData("InteractiveWebAssembly", true, "Attribute")]
        [InlineData("InteractiveServer", true, "Attribute")]
        [InlineData("InteractiveAuto", false, "Attribute")]
        [InlineData("InteractiveAuto", true, "Element")]
        [InlineData("InteractiveAuto", true, "Central")]
        [InlineData("InteractiveAuto", true, "Attribute", "$(MSBuildProjectName)")]
        [InlineData("InteractiveAuto", true, "Attribute", "$(MSBuildProjectName.Replace('-', '_'))")]
        [InlineData("InteractiveAuto", true, "Attribute", "Custom.Client")]
        [InlineData("new InteractiveWebAssemblyRenderMode(prerender: false)", true, "Attribute")]
        [InlineData("new InteractiveAutoRenderMode(prerender: false)", true, "Attribute")]
        [InlineData("new InteractiveServerRenderMode(prerender: false)", true, "Attribute")]
        public async Task ModifyFiles_UpdatesBothProjectsWithoutServerDependenciesInClient(
            string renderMode, bool global, string packageVersionStyle, string rootNamespace = null)
        {
            var isClientHosted = !renderMode.Contains("InteractiveServer", StringComparison.Ordinal);
            var client = isClientHosted
                ? AddProject("Client", "Microsoft.NET.Sdk.BlazorWebAssembly",
                    "using Microsoft.AspNetCore.Components.WebAssembly.Hosting;\nvar clientBuilder = WebAssemblyHostBuilder.CreateDefault(args);\nawait clientBuilder.Build().RunAsync();")
                : null;
            var server = AddProject("Server", "Microsoft.NET.Sdk.Web",
                isClientHosted
                    ? "var builder = WebApplication.CreateBuilder(args);\nbuilder.Services.AddRazorComponents().AddInteractiveWebAssemblyComponents();\nvar app = builder.Build();\napp.Run();"
                    : "var builder = WebApplication.CreateBuilder(args);\nbuilder.Services.AddRazorComponents().AddInteractiveServerComponents();\nvar app = builder.Build();\napp.Run();");
            if (packageVersionStyle == "Element")
            {
                Write("Client", "Client.csproj",
                    "<Project Sdk=\"Microsoft.NET.Sdk.BlazorWebAssembly\"><ItemGroup><PackageReference Include=\"Microsoft.AspNetCore.Components.WebAssembly\"><Version>10.0.14</Version></PackageReference></ItemGroup></Project>");
            }
            else if (packageVersionStyle == "Central")
            {
                Write("Client", "Directory.Packages.props",
                    "<Project><PropertyGroup><ManagePackageVersionsCentrally>true</ManagePackageVersionsCentrally></PropertyGroup><ItemGroup><PackageVersion Include=\"Microsoft.AspNetCore.Components.WebAssembly\" Version=\"10.0.14\" /><PackageVersion Include=\"Microsoft.AspNetCore.Components.WebAssembly.Authentication\" Version=\"10.0.14\" /></ItemGroup></Project>");
                Write("Client", "Client.csproj",
                    "<Project Sdk=\"Microsoft.NET.Sdk.BlazorWebAssembly\"><ItemGroup><PackageReference Include=\"Microsoft.AspNetCore.Components.WebAssembly\" /><PackageReference Include=\"Microsoft.AspNetCore.Components.WebAssembly.Authentication\" /></ItemGroup></Project>");
            }
            if (rootNamespace != null)
            {
                var clientXml = XDocument.Parse(Read("Client", "Client.csproj"));
                clientXml.Root.Add(new XElement("PropertyGroup", new XElement("RootNamespace", rootNamespace)));
                Write("Client", "Client.csproj", clientXml.ToString());
            }
            var uiProject = (global && isClientHosted) ? "Client" : "Server";
            var components = (global && isClientHosted) ? "" : "Components";
            Write(uiProject, Path.Combine(components, "Routes.razor"),
                "<Router AppAssembly=\"typeof(Program).Assembly\"><Found Context=\"routeData\"><RouteView RouteData=\"routeData\" DefaultLayout=\"typeof(Layout.MainLayout)\" /></Found></Router>");
            Write(uiProject, Path.Combine(components, "Layout", "NavMenu.razor"),
                "<div class=\"top-row ps-3 navbar navbar-dark\">\n</div>\n<nav>\n        <div class=\"nav-item px-3\">\n            <NavLink class=\"nav-link\" href=\"weather\">\n                <span class=\"bi bi-list-nested-nav-menu\" aria-hidden=\"true\"></span> Weather\n            </NavLink>\n        </div>\n</nav>");
            Write(uiProject, Path.Combine(components, "Layout", "NavMenu.razor.css"), ".nav-item {\n}");
            Write("Server", Path.Combine("Components", "_Imports.razor"),
                "@using Microsoft.AspNetCore.Components.Forms\n@using Server.Components.Account.Shared\n@using Client");
            if (client != null)
            {
                Write("Client", "_Imports.razor", "@using Microsoft.AspNetCore.Components.Forms");
            }
            Write("Server", Path.Combine("Components", "App.razor"), global
                ? $"<HeadOutlet @rendermode=\"{renderMode}\" /><Routes @rendermode=\"{renderMode}\" /></body>"
                : "<HeadOutlet /><Routes /></body>");

            var generator = CreateGenerator(client);
            Assert.All(_productionWorkspace.CurrentSolution.Projects, project => Assert.Empty(project.ProjectReferences));
            await generator.ModifyFilesAsync(_model);

            var serverProgram = Read("Server", "Program.cs");
            if (isClientHosted)
            {
                var clientProgram = Read("Client", "Program.cs");
                Assert.Contains("AddAuthenticationStateSerialization()", serverProgram);
                Assert.DoesNotContain("AddAuthorizationCore()", serverProgram);
                Assert.Contains("AddAuthorizationCore()", clientProgram);
                Assert.Contains("AddCascadingAuthenticationState()", clientProgram);
                Assert.Contains("AddAuthenticationStateDeserialization()", clientProgram);
                Assert.DoesNotContain("Server.", clientProgram);
                Assert.DoesNotContain("Identity", clientProgram);
                Assert.Contains("clientBuilder.Services.AddAuthorizationCore()", clientProgram);
                var imports = Read("Client", "_Imports.razor");
                Assert.Contains("Microsoft.AspNetCore.Components.Authorization", imports);
                Assert.DoesNotContain("Microsoft.AspNetCore.Components.Endpoints", imports);
                Assert.Contains("forceLoad: true", Read("Client", "IdentityRedirectToLogin.razor"));
                var redirect = Read("Client", "IdentityRedirectToLogin.razor");
                var namespaceDirective = redirect.Split('\n').First().Trim();
                Assert.StartsWith("@namespace ", namespaceDirective);
                var redirectNamespace = namespaceDirective.Substring("@namespace ".Length);
                Assert.False(SyntaxFactory.ParseName(redirectNamespace).ContainsDiagnostics, namespaceDirective);
                Assert.Equal("BlazorIdentityClient", redirectNamespace);
                if (uiProject == "Client")
                {
                    Assert.Contains($"@using {redirectNamespace}", Read("Client", "Routes.razor"));
                }
                Assert.False(Directory.Exists(Path.Combine(_root, "Client", "Components", "Account")));
                var authenticationPackage = XDocument.Parse(Read("Client", "Client.csproj"))
                    .Descendants("PackageReference").Single(package => (string)package.Attribute("Include") ==
                        "Microsoft.AspNetCore.Components.WebAssembly.Authentication");
                Assert.Equal(packageVersionStyle == "Central" ? null : "10.0.14", (string)authenticationPackage.Attribute("Version"));
            }
            else
            {
                Assert.DoesNotContain("AddAuthenticationStateSerialization()", serverProgram);
            }

            Assert.Contains("AuthorizeRouteView", Read(uiProject, Path.Combine(components, "Routes.razor")));
            if (uiProject == "Client")
            {
                Assert.DoesNotContain("Server.Components.Account", Read(uiProject, Path.Combine(components, "Routes.razor")));
            }
            else
            {
                var routes = Read("Server", Path.Combine(components, "Routes.razor"));
                Assert.Contains("@using Server.Components.Account.Shared", routes);
                Assert.DoesNotContain("@using Client", routes);
                var clientRedirect = isClientHosted ? Read("Client", "IdentityRedirectToLogin.razor").Split('\n').First().Trim() : null;
                if (clientRedirect != null)
                {
                    Assert.DoesNotContain(clientRedirect.Replace("@namespace ", "@using "), routes);
                }
            }
            var nav = Read(uiProject, Path.Combine(components, "Layout", "NavMenu.razor"));
            Assert.Contains("Account/Register", nav);
            Assert.Contains("Account/Login", nav);
            Assert.Contains("AuthorizeView", nav);
            Assert.Contains("bi-lock-nav-menu", Read(uiProject, Path.Combine(components, "Layout", "NavMenu.razor.css")));
            var serverImports = Read("Server", Path.Combine("Components", "_Imports.razor"));
            Assert.Contains("Microsoft.AspNetCore.Components.Endpoints", serverImports);
            Assert.Contains("Microsoft.AspNetCore.Components.Authorization", serverImports);

            var serverApp = Read("Server", Path.Combine("Components", "App.razor"));
            if (global)
            {
                Assert.Contains("<HeadOutlet @rendermode=\"PageRenderMode\" />", serverApp);
                Assert.Contains("<Routes @rendermode=\"PageRenderMode\" />", serverApp);
                Assert.Contains($"HttpContext.AcceptsInteractiveRouting() ? {renderMode} : null;", serverApp);
            }
            else
            {
                Assert.DoesNotContain("PageRenderMode", serverApp);
            }

            var clientProgramBeforeRepeat = isClientHosted ? Read("Client", "Program.cs") : null;
            var clientProjectBeforeRepeat = isClientHosted ? Read("Client", "Client.csproj") : null;
            await generator.ModifyFilesAsync(_model);
            Assert.Equal(serverProgram, Read("Server", "Program.cs"));
            if (isClientHosted)
            {
                Assert.Equal(clientProgramBeforeRepeat, Read("Client", "Program.cs"));
                Assert.Equal(clientProjectBeforeRepeat, Read("Client", "Client.csproj"));
            }
        }

        [Fact]
        public async Task ModifyFiles_AddsSerializationWhenRazorComponentsHasOptions()
        {
            var client = AddProject("Client", "Microsoft.NET.Sdk.BlazorWebAssembly",
                "var builder = WebAssemblyHostBuilder.CreateDefault(args);\nawait builder.Build().RunAsync();");
            AddProject("Server", "Microsoft.NET.Sdk.Web",
                "var builder = WebApplication.CreateBuilder(args);\nbuilder.Services.AddRazorComponents(options => options.DetailedErrors = true).AddInteractiveWebAssemblyComponents();\nvar app = builder.Build();\napp.Run();");
            Write("Client", "_Imports.razor", "@using Microsoft.AspNetCore.Components.Forms");

            await CreateGenerator(client).ModifyFilesAsync(_model);

            var program = Read("Server", "Program.cs");
            Assert.Contains("AddRazorComponents(options => options.DetailedErrors = true)", program);
            Assert.Contains("AddAuthenticationStateSerialization()", program);
        }

        [Fact]
        public async Task ModifyFiles_PreservesExistingClientRedirect()
        {
            var client = AddProject("Client", "Microsoft.NET.Sdk.BlazorWebAssembly",
                "var builder = WebAssemblyHostBuilder.CreateDefault(args);\nawait builder.Build().RunAsync();");
            AddProject("Server", "Microsoft.NET.Sdk.Web",
                "var builder = WebApplication.CreateBuilder(args);\nbuilder.Services.AddRazorComponents();\nvar app = builder.Build();\napp.Run();");
            Write("Client", "_Imports.razor", "@using Client\n@using Microsoft.AspNetCore.Components.Forms");
            const string existingRedirect = "@namespace Client\n<p>Existing redirect</p>";
            Write("Client", "RedirectToLogin.razor", existingRedirect);
            Write("Client", "Routes.razor",
                "<RouteView RouteData=\"routeData\" DefaultLayout=\"typeof(Layout.MainLayout)\" />");
            var generator = CreateGenerator(client);

            await generator.ModifyFilesAsync(_model);

            Assert.Equal(existingRedirect, Read("Client", "RedirectToLogin.razor"));
            Assert.Contains("@namespace BlazorIdentityClient", Read("Client", "IdentityRedirectToLogin.razor"));
            Assert.Contains("forceLoad: true", Read("Client", "IdentityRedirectToLogin.razor"));
            var routes = Read("Client", "Routes.razor");
            Assert.Contains("<BlazorIdentityClient.IdentityRedirectToLogin />", routes);
            Assert.DoesNotContain("<RedirectToLogin />", routes);
            await generator.ModifyFilesAsync(_model);
            Assert.Equal(routes, Read("Client", "Routes.razor"));
            Assert.Equal(existingRedirect, Read("Client", "RedirectToLogin.razor"));
        }

        [Theory]
        [InlineData("InteractiveWebAssembly", "new InteractiveWebAssemblyRenderMode(prerender: false)", " />")]
        [InlineData("new InteractiveAutoRenderMode(prerender: false)", "InteractiveAuto", " />")]
        [InlineData("InteractiveServer", "InteractiveServer", "/>")]
        [InlineData("InteractiveWebAssembly", "new InteractiveWebAssemblyRenderMode(prerender: false)", "/>")]
        public async Task ModifyFiles_ConditionsEachRenderModeAndPreservesTagAttributes(
            string headMode, string routesMode, string closing)
        {
            AddProject("Server", "Microsoft.NET.Sdk.Web",
                "var builder = WebApplication.CreateBuilder(args);\nbuilder.Services.AddRazorComponents();\nvar app = builder.Build();\napp.Run();");
            var appPath = Path.Combine("Components", "App.razor");
            Write("Server", appPath,
                $"<HeadOutlet @rendermode=\"{headMode}\"{closing}<Routes data-test=\"routes\" @rendermode=\"{routesMode}\" {closing}</body>");
            var generator = CreateGenerator();

            await generator.ModifyFilesAsync(_model);

            var app = Read("Server", appPath);
            Assert.DoesNotContain($"@rendermode=\"{headMode}\"", app);
            Assert.DoesNotContain($"@rendermode=\"{routesMode}\"", app);
            Assert.Contains($"HttpContext.AcceptsInteractiveRouting() ? {headMode} : null", app);
            Assert.Contains($"HttpContext.AcceptsInteractiveRouting() ? {routesMode} : null", app);
            Assert.Contains("data-test=\"routes\"", app);
            await generator.ModifyFilesAsync(_model);
            Assert.Equal(app, Read("Server", appPath));
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

        [Theory]
        [InlineData("var marker = 1;")]
        [InlineData("class Startup { public static void Main(string[] args) { } }")]
        public async Task ModifyProgram_ResolvesRenamedStartupWithProductionCompilationOptions(string source)
        {
            AddProject("Server", "Microsoft.NET.Sdk.Web", source);
            var startupPath = Path.Combine(_root, "Server", "Host.cs");
            File.Move(Path.Combine(_root, "Server", "Program.cs"), startupPath);
            var document = _workspace.CurrentSolution.Projects.Single().Documents.Single();
            Assert.True(_workspace.TryApplyChanges(_workspace.CurrentSolution
                .WithDocumentName(document.Id, "Host.cs").WithDocumentFilePath(document.Id, startupPath)));
            var generator = CreateGenerator();
            var project = _productionWorkspace.CurrentSolution.Projects.Single()
                .AddMetadataReference(MetadataReference.CreateFromFile(typeof(object).Assembly.Location));
            Assert.Equal(OutputKind.ConsoleApplication, (await project.GetCompilationAsync()).Options.OutputKind);

            await generator.ModifyProgramCsAsync(new CodeFile
            {
                FileName = "Program.cs",
                Usings = new[] { "System" }
            }, _model, project);

            Assert.Contains("using System;", File.ReadAllText(startupPath));
            Assert.False(File.Exists(Path.Combine(_root, "Server", "Program.cs")));
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public async Task MissingClientPackageVersion_FailsBeforeWritingFiles(bool generateCode)
        {
            AddProject("Server", "Microsoft.NET.Sdk.Web",
                "var builder = WebApplication.CreateBuilder(args);\nbuilder.Services.AddRazorComponents().AddInteractiveWebAssemblyComponents();\nvar app = builder.Build();\napp.Run();");
            var client = AddProject("Client", "Microsoft.NET.Sdk.BlazorWebAssembly",
                "var builder = WebAssemblyHostBuilder.CreateDefault(args);\nawait builder.Build().RunAsync();");
            Write("Client", "Client.csproj",
                "<Project Sdk=\"Microsoft.NET.Sdk.BlazorWebAssembly\"><ItemGroup><PackageReference Include=\"Microsoft.AspNetCore.Components.WebAssembly\" /></ItemGroup></Project>");
            Write("Client", "_Imports.razor", "@using Microsoft.AspNetCore.Components.Forms");
            Write("Server", Path.Combine("Components", "App.razor"),
                "<HeadOutlet @rendermode=\"InteractiveAuto\" /><Routes @rendermode=\"InteractiveAuto\" /></body>");
            var originalFiles = Directory.GetFiles(_root, "*", SearchOption.AllDirectories)
                .ToDictionary(path => path, File.ReadAllText);
            var generator = CreateGenerator(client);

            var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => generateCode
                ? generator.GenerateCode(new BlazorIdentityCommandLineModel())
                : generator.ModifyFilesAsync(_model));

            Assert.Contains("Microsoft.AspNetCore.Components.WebAssembly.Authentication", exception.Message);
            Assert.Contains("before scaffolding", exception.Message);
            Assert.Equal(originalFiles.Keys.OrderBy(path => path),
                Directory.GetFiles(_root, "*", SearchOption.AllDirectories).OrderBy(path => path));
            foreach (var file in originalFiles)
            {
                Assert.Equal(file.Value, File.ReadAllText(file.Key));
            }
        }

        [SkippableTheory]
        [InlineData("Auto")]
        [InlineData("WebAssembly")]
        [InlineData("Server")]
        [InlineData("Auto", false)]
        [InlineData("WebAssembly", false)]
        [InlineData("WebAssembly", true, true, "$(MSBuildProjectName)")]
        [InlineData("Auto", true, true, "$(MSBuildProjectName.Replace('-', '_'))")]
        [InlineData("Server", true, true)]
        [InlineData("Auto", true, false, null, true)]
        [InlineData("WebAssembly", true, false, null, true)]
        [InlineData("Server", true, false, null, true)]
        public async Task StockGlobalTemplate_ScaffoldsAndBuildsProjects(
            string interactivity, bool global = true, bool explicitRenderMode = false, string rootNamespace = null,
            bool reviewEdgeCases = false)
        {
            Skip.If(string.IsNullOrEmpty(Environment.GetEnvironmentVariable("SCAFFOLDING_RunSkippableTests")));
            Directory.CreateDirectory(_root);
            var isClientHosted = interactivity != "Server";
            var templateArguments = new List<string>
            {
                "new", "blazor", "--name", "Server", "--output", isClientHosted ? _root : Path.Combine(_root, "Server"),
                "--framework", "net10.0", "--interactivity", interactivity, "--no-restore"
            };
            if (global)
            {
                templateArguments.Add("--all-interactive");
            }
            await RunDotnet(templateArguments.ToArray());
            if (rootNamespace != null)
            {
                var clientPath = Path.Combine(_root, "Server.Client", "Server.Client.csproj");
                var clientXml = XDocument.Load(clientPath);
                clientXml.Root.Add(new XElement("PropertyGroup", new XElement("RootNamespace", rootNamespace)));
                clientXml.Save(clientPath);
            }
            var renderMode = explicitRenderMode
                ? $"new Interactive{interactivity}RenderMode(prerender: false)"
                : $"Interactive{interactivity}";
            var appPath = Path.Combine("Components", "App.razor");
            if (explicitRenderMode)
            {
                Write("Server", appPath, Read("Server", appPath)
                    .Replace($"@rendermode=\"Interactive{interactivity}\"", $"@rendermode=\"{renderMode}\""));
            }
            if (reviewEdgeCases)
            {
                Write("Server", appPath, Read("Server", appPath).Replace(
                    $"<Routes @rendermode=\"Interactive{interactivity}\" />",
                    $"<Routes @rendermode=\"new Interactive{interactivity}RenderMode(prerender: false)\"/>"));
                Write("Server", "Program.cs", Read("Server", "Program.cs").Replace(
                    "AddRazorComponents()", "AddRazorComponents(options => options.DetailedErrors = true)"));
                if (isClientHosted)
                {
                    Write("Server.Client", "RedirectToLogin.razor", "@namespace Server.Client\n<p>Existing redirect</p>");
                }
            }
            var importsPath = Path.Combine("Components", "_Imports.razor");
            Write("Server", importsPath, Read("Server", importsPath) + "\n@using Server.Components.Account.Shared\n");
            var client = isClientHosted ? AddExistingProject("Server.Client") : null;
            var server = AddExistingProject("Server");
            var serverPath = Path.Combine(_root, "Server", "Server.csproj");
            var serverXml = XDocument.Load(serverPath);
            var version = isClientHosted
                ? (string)XDocument.Load(Path.Combine(_root, "Server.Client", "Server.Client.csproj")).Descendants("PackageReference").Single().Attribute("Version")
                : "10.0.0";
            serverXml.Root.Add(new XElement("ItemGroup",
                new XElement("PackageReference", new XAttribute("Include", "Microsoft.AspNetCore.Identity.EntityFrameworkCore"), new XAttribute("Version", version)),
                new XElement("PackageReference", new XAttribute("Include", "Microsoft.EntityFrameworkCore.Sqlite"), new XAttribute("Version", version))));
            serverXml.Save(serverPath);
            var context = CreateProjectContext(client);
            context.SetupGet(c => c.RootNamespace).Returns("Server");
            context.SetupGet(c => c.PackageDependencies).Returns(new[]
            {
                new DependencyDescription("Microsoft.VisualStudio.Web.CodeGenerators.Mvc", "1.0", AppContext.BaseDirectory,
                    "net10.0", DependencyType.Package, true)
            });
            var app = new Mock<IApplicationInfo>();
            app.SetupGet(a => a.ApplicationBasePath).Returns(Path.Combine(_root, "Server"));
            _productionWorkspace = new RoslynWorkspace(context.Object);
            Assert.All(_productionWorkspace.CurrentSolution.Projects, project => Assert.Empty(project.ProjectReferences));
            var generator = new BlazorIdentityGenerator(app.Object, new ModelTypesLocator(_productionWorkspace),
                Mock.Of<ILogger>(), DefaultFileSystem.Instance, Mock.Of<IFilesLocator>(), context.Object,
                Mock.Of<IEntityFrameworkService>(), Mock.Of<ICodeGenAssemblyLoadContext>(), _productionWorkspace);
            _model.BaseOutputPath = Path.Combine(_root, "Server");
            _model.BlazorLayoutNamespace = isClientHosted && global ? "Server.Client.Layout.MainLayout" : "Server.Components.Layout.MainLayout";
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

            var clientRoutes = isClientHosted && global;
            var routesProject = clientRoutes ? "Server.Client" : "Server";
            var routesPath = clientRoutes ? "Routes.razor" : Path.Combine("Components", "Routes.razor");
            var navMenuPath = clientRoutes ? Path.Combine("Layout", "NavMenu.razor") : Path.Combine("Components", "Layout", "NavMenu.razor");

            var routesContent = Read(routesProject, routesPath);
            Assert.Contains("AuthorizeRouteView", routesContent);
            if (clientRoutes)
            {
                Assert.DoesNotContain("Server.Components.Account", routesContent);
            }
            Assert.Contains("Account/Register", Read(routesProject, navMenuPath));
            Assert.Contains("Account/Login", Read(routesProject, navMenuPath));
            Assert.True(File.Exists(Path.Combine(_root, "Server", "Components", "Account", "Pages", "Login.razor")));
            Assert.True(File.Exists(Path.Combine(_root, "Server", "Components", "Account", "Pages", "Register.razor")));
            if (isClientHosted)
            {
                Assert.False(Directory.Exists(Path.Combine(_root, "Server.Client", "Components", "Account")));
                Assert.Contains("AddAuthenticationStateSerialization()", Read("Server", "Program.cs"));
                Assert.Contains("AddAuthenticationStateDeserialization()", Read("Server.Client", "Program.cs"));
            }
            if (global)
            {
                Assert.Contains($"? {renderMode} : null", Read("Server", appPath));
            }
            else
            {
                Assert.DoesNotContain("PageRenderMode", Read("Server", appPath));
            }
            if (reviewEdgeCases)
            {
                Assert.Contains($"HttpContext.AcceptsInteractiveRouting() ? new Interactive{interactivity}RenderMode(prerender: false) : null",
                    Read("Server", appPath));
                if (isClientHosted)
                {
                    Assert.Contains("<BlazorIdentityClient.IdentityRedirectToLogin />", routesContent);
                    Assert.Contains("Existing redirect", Read("Server.Client", "RedirectToLogin.razor"));
                }
            }
            Assert.Contains("[ExcludeFromInteractiveRouting]", Read("Server", Path.Combine("Components", "Account", "Pages", "_Imports.razor")));
            await RunDotnet("build", serverPath, "-p:UseSharedCompilation=false", "-m:1", "-nr:false", "--nologo");
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
            catch (TimeoutException exception)
            {
                process.Kill(entireProcessTree: true);
                await process.WaitForExitAsync();
                throw new TimeoutException(
                    $"dotnet {string.Join(" ", arguments)} timed out:\n{await output}\n{await error}", exception);
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

        private Mock<IProjectContext> CreateProjectContext(Project clientProject = null)
        {
            var server = _workspace.CurrentSolution.Projects.Single(project => project.AssemblyName == "Server");
            var context = new Mock<IProjectContext>();
            context.SetupGet(c => c.AssemblyName).Returns("Server");
            context.SetupGet(c => c.ProjectName).Returns("Server");
            context.SetupGet(c => c.ProjectFullPath).Returns(server.FilePath);
            context.SetupGet(c => c.CompilationItems).Returns(server.Documents.Select(document => document.FilePath));
            context.SetupGet(c => c.CompilationAssemblies).Returns(Array.Empty<ResolvedReference>());
            context.SetupGet(c => c.ProjectCapabilities).Returns(Array.Empty<string>());
            context.SetupGet(c => c.ProjectReferenceInformation).Returns(clientProject == null
                ? Array.Empty<ProjectReferenceInformation>()
                : new[]
                {
                    new ProjectReferenceInformation
                    {
                        FullPath = clientProject.FilePath,
                        ProjectName = clientProject.Name,
                        AssemblyName = clientProject.AssemblyName,
                        CompilationItems = clientProject.Documents.Select(document => document.FilePath).ToList()
                    }
                });
            return context;
        }

        private BlazorIdentityGenerator CreateGenerator(Project clientProject = null)
        {
            var context = CreateProjectContext(clientProject);
            _productionWorkspace = new RoslynWorkspace(context.Object);
            return new BlazorIdentityGenerator(Mock.Of<IApplicationInfo>(), new ModelTypesLocator(_productionWorkspace),
                Mock.Of<ILogger>(), DefaultFileSystem.Instance, Mock.Of<IFilesLocator>(), context.Object,
                Mock.Of<IEntityFrameworkService>(), Mock.Of<ICodeGenAssemblyLoadContext>(), _productionWorkspace);
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
            _productionWorkspace?.Dispose();
            _workspace.Dispose();
            if (Directory.Exists(_root))
            {
                Directory.Delete(_root, recursive: true);
            }
        }
    }
}
