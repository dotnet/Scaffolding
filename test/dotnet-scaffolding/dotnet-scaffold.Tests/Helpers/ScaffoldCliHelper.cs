// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Xunit;

namespace Microsoft.DotNet.Tools.Scaffold.Tests.Helpers;

/// <summary>
/// Shared helper for invoking the dotnet-scaffold CLI tool from integration tests.
/// Uses <c>dotnet run --project</c> to invoke the tool from source, avoiding global tool installation.
/// </summary>
internal static class ScaffoldCliHelper
{
    /// <summary>
    /// Gets the repository root directory by navigating up from the test assembly output path.
    /// The Arcade build layout is: {repoRoot}/artifacts/bin/{project}/{Config}/{TFM}/{assembly}.dll
    /// </summary>
    private static string GetRepoRoot()
    {
        var assemblyLocation = Assembly.GetExecutingAssembly().Location;
        var assemblyDirectory = Path.GetDirectoryName(assemblyLocation)!;
        // Navigate from artifacts/bin/dotnet-scaffold.Tests/{Config}/{TFM}/ up to repo root
        return Path.GetFullPath(Path.Combine(assemblyDirectory, "..", "..", "..", "..", ".."));
    }

    /// <summary>
    /// Gets the absolute path to the dotnet-scaffold.csproj source project.
    /// </summary>
    public static string GetScaffoldProjectPath()
    {
        return Path.Combine(GetRepoRoot(), "src", "dotnet-scaffolding", "dotnet-scaffold", "dotnet-scaffold.csproj");
    }

    /// <summary>
    /// Gets the path to the dotnet executable.
    /// On CI, the Arcade build system installs the correct .NET SDK at {repoRoot}/.dotnet/.
    /// This method checks multiple sources in priority order:
    /// 1. DOTNET_INSTALL_DIR environment variable (set by Arcade's eng/common/tools.ps1)
    /// 2. {repoRoot}/.dotnet/ directory (standard Arcade layout)
    /// 3. The directory of the currently-running dotnet process (the host running the tests)
    /// 4. Falls back to "dotnet" (resolved via PATH)
    /// </summary>
    public static string GetDotNetPath()
    {
        // 1. Check DOTNET_INSTALL_DIR — Arcade always sets this
        var installDir = System.Environment.GetEnvironmentVariable("DOTNET_INSTALL_DIR");
        if (!string.IsNullOrEmpty(installDir))
        {
            var candidate = FindDotNetInDir(installDir);
            if (candidate != null) return candidate;
        }

        // 2. Check {repoRoot}/.dotnet/
        var repoRoot = GetRepoRoot();
        var dotnetDir = Path.Combine(repoRoot, ".dotnet");
        var fromRepo = FindDotNetInDir(dotnetDir);
        if (fromRepo != null) return fromRepo;

        // 3. Check the running process's directory
        var currentProcess = System.Diagnostics.Process.GetCurrentProcess();
        var processDir = Path.GetDirectoryName(currentProcess.MainModule?.FileName);
        if (!string.IsNullOrEmpty(processDir))
        {
            var fromProcess = FindDotNetInDir(processDir);
            if (fromProcess != null) return fromProcess;
        }

        return "dotnet";
    }

    private static string? FindDotNetInDir(string directory)
    {
        if (!Directory.Exists(directory)) return null;

        var dotnetExe = Path.Combine(directory, "dotnet.exe");
        if (File.Exists(dotnetExe)) return dotnetExe;

        var dotnetBin = Path.Combine(directory, "dotnet");
        if (File.Exists(dotnetBin)) return dotnetBin;

        return null;
    }

    /// <summary>
    /// Configures a <see cref="ProcessStartInfo"/> for running dotnet commands
    /// in integration tests. Sets the resolved dotnet path and ensures the child
    /// process has a consistent environment by pointing DOTNET_ROOT to the same
    /// dotnet installation directory. Does NOT restrict multilevel lookup so the
    /// host can resolve SDKs and runtimes from both the local and global installs.
    /// Clears MSBUILD_EXE_PATH to prevent the test host's MSBuild context from
    /// leaking into the child build process.
    /// </summary>
    private static void ConfigureDotNetEnvironment(ProcessStartInfo startInfo)
    {
        var dotnetPath = GetDotNetPath();
        startInfo.FileName = dotnetPath;

        if (dotnetPath != "dotnet")
        {
            var dotnetRoot = Path.GetDirectoryName(dotnetPath)!;
            startInfo.Environment["DOTNET_ROOT"] = dotnetRoot;
        }

        // Clear variables that the test host process may have set and which
        // can poison child MSBuild invocations with wrong assembly versions.
        startInfo.Environment.Remove("MSBUILD_EXE_PATH");
        startInfo.Environment.Remove("MSBuildSDKsPath");
        startInfo.Environment.Remove("MSBuildExtensionsPath");
    }

    /// <summary>
    /// Detects the build configuration (Debug/Release) from the test assembly's output path.
    /// The Arcade build layout is: artifacts/bin/{project}/{Config}/{TFM}/{assembly}.dll
    /// so the configuration is the parent of the TFM directory.
    /// Falls back to "Debug" if the configuration cannot be determined.
    /// </summary>
    public static string GetBuildConfiguration()
    {
        var assemblyLocation = Assembly.GetExecutingAssembly().Location;
        var assemblyDirectory = Path.GetDirectoryName(assemblyLocation)!;
        // assemblyDirectory = .../artifacts/bin/dotnet-scaffold.Tests/{Config}/{TFM}
        // Parent = {Config}, GrandParent = dotnet-scaffold.Tests
        var configDir = Path.GetFileName(Path.GetDirectoryName(assemblyDirectory));
        if (configDir != null &&
            (configDir.Equals("Release", System.StringComparison.OrdinalIgnoreCase) ||
             configDir.Equals("Debug", System.StringComparison.OrdinalIgnoreCase)))
        {
            return configDir;
        }
        return "Debug";
    }

    /// <summary>
    /// Runs a dotnet-scaffold CLI command by invoking <c>dotnet run --no-build -c {config} --project {scaffoldCsproj} --framework {framework} -- aspnet {command} {args}</c>.
    /// Uses <c>--no-build</c> because the solution must already be built before running tests.
    /// The build configuration is auto-detected from the test assembly output path so the correct
    /// Debug or Release build of the tool is used.
    /// The <paramref name="targetFramework"/> controls which TFM of the multi-targeted dotnet-scaffold tool is executed,
    /// simulating a machine that only has that .NET version installed.
    /// </summary>
    /// <param name="targetFramework">The target framework moniker to run the tool under (e.g., "net8.0", "net9.0", "net10.0", "net11.0").</param>
    /// <param name="command">The scaffold sub-command (e.g., "minimalapi", "mvccontroller", "blazor-empty").</param>
    /// <param name="args">CLI arguments for the command (e.g., "--project", path, "--name", "Foo").</param>
    /// <returns>A tuple of (ExitCode, StandardOutput, StandardError).</returns>
    public static async Task<(int ExitCode, string Output, string Error)> RunScaffoldAsync(string targetFramework, string command, params string[] args)
    {
        var scaffoldCsproj = GetScaffoldProjectPath();
        var configuration = GetBuildConfiguration();
        var cliArgs = $"run --no-build -c {configuration} --project \"{scaffoldCsproj}\" --framework {targetFramework} -- aspnet {command} {string.Join(" ", args.Select(a => a.Contains(' ') ? $"\"{a}\"" : a))}";

        var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                Arguments = cliArgs,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            }
        };
        ConfigureDotNetEnvironment(process.StartInfo);

        process.Start();
        var stdoutTask = process.StandardOutput.ReadToEndAsync();
        var stderrTask = process.StandardError.ReadToEndAsync();
        await Task.WhenAll(stdoutTask, stderrTask);
        await process.WaitForExitAsync();
        return (process.ExitCode, stdoutTask.Result, stderrTask.Result);
    }

    /// <summary>
    /// Runs <c>dotnet build</c> in the specified working directory.
    /// </summary>
    public static async Task<(int ExitCode, string Output, string Error)> RunBuildAsync(string workingDirectory)
    {
        var buildProcess = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                Arguments = "build",
                WorkingDirectory = workingDirectory,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            }
        };
        ConfigureDotNetEnvironment(buildProcess.StartInfo);
        buildProcess.Start();
        var buildStdoutTask = buildProcess.StandardOutput.ReadToEndAsync();
        var buildStderrTask = buildProcess.StandardError.ReadToEndAsync();
        await Task.WhenAll(buildStdoutTask, buildStderrTask);
        await buildProcess.WaitForExitAsync();
        return (buildProcess.ExitCode, buildStdoutTask.Result, buildStderrTask.Result);
    }

    /// <summary>
    /// Runs <c>dotnet build -f {targetFramework}</c> in the specified working directory.
    /// Used by integration test base classes to build test projects targeting a specific framework.
    /// Uses the Arcade dotnet installation when available.
    /// </summary>
    public static async Task<(int ExitCode, string Output, string Error)> RunBuildForFrameworkAsync(string workingDirectory, string targetFramework)
    {
        var buildProcess = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                Arguments = $"build -f {targetFramework}",
                WorkingDirectory = workingDirectory,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            }
        };
        ConfigureDotNetEnvironment(buildProcess.StartInfo);
        buildProcess.Start();
        var fwBuildStdoutTask = buildProcess.StandardOutput.ReadToEndAsync();
        var fwBuildStderrTask = buildProcess.StandardError.ReadToEndAsync();
        await Task.WhenAll(fwBuildStdoutTask, fwBuildStderrTask);
        await buildProcess.WaitForExitAsync();
        return (buildProcess.ExitCode, fwBuildStdoutTask.Result, fwBuildStderrTask.Result);
    }

    /// <summary>
    /// Generates a minimal .csproj file content for a Web SDK project targeting the specified framework.
    /// </summary>
    public static string GetWebProjectContent(string targetFramework) =>
        $@"<Project Sdk=""Microsoft.NET.Sdk.Web"">
  <PropertyGroup>
    <TargetFramework>{targetFramework}</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
  </PropertyGroup>
</Project>";

    /// <summary>
    /// Generates a minimal Program.cs for a web application.
    /// Required by scaffolders that modify Program.cs (minimalapi, identity, entra-id, etc.).
    /// </summary>
    public static string GetMinimalProgramCs() =>
        @"var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();
app.Run();
";

    /// <summary>
    /// Generates a minimal Program.cs for a Blazor web application.
    /// Includes the Components namespace using directive required by scaffolded code
    /// that adds MapRazorComponents&lt;App&gt;() referencing Components/App.razor.
    /// </summary>
    /// <param name="projectName">The project name (root namespace), e.g. "TestProject".</param>
    public static string GetBlazorProgramCs(string projectName) =>
        $@"using {projectName}.Components;
var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();
app.Run();
";

    /// <summary>
    /// Generates a simple POCO model class.
    /// Required by CRUD scaffolders that need a model class to exist in the project.
    /// </summary>
    /// <param name="rootNamespace">The root namespace (typically the project name).</param>
    /// <param name="modelName">The model class name (e.g., "TestModel").</param>
    public static string GetModelClassContent(string rootNamespace, string modelName) =>
        $@"namespace {rootNamespace}.Models;

public class {modelName}
{{
    public int Id {{ get; set; }}
    public string? Name {{ get; set; }}
    public string? Description {{ get; set; }}
}}
";

    /// <summary>
    /// Gets a minimal _Imports.razor for Blazor components.
    /// Required by Blazor CRUD scaffolders so that InputText, EditForm, etc. are recognized.
    /// </summary>
    public static string GetBlazorImportsRazor() =>
        @"@using Microsoft.AspNetCore.Components.Forms
@using Microsoft.AspNetCore.Components.Routing
@using Microsoft.AspNetCore.Components.Web
@using static Microsoft.AspNetCore.Components.Web.RenderMode
";

    /// <summary>
    /// Gets a minimal App.razor component.
    /// Required because the scaffold adds MapRazorComponents&lt;App&gt;() to Program.cs.
    /// </summary>
    public static string GetBlazorAppRazor() =>
        @"<!DOCTYPE html>
<html>
<head><title>Test</title><HeadOutlet /></head>
<body><Routes /><script src=""_framework/blazor.web.js""></script></body>
</html>
";

    /// <summary>
    /// Gets a minimal Routes.razor component.
    /// Required by App.razor.
    /// </summary>
    public static string GetBlazorRoutesRazor() =>
        @"@using Microsoft.AspNetCore.Components.Routing

<Router AppAssembly=""typeof(Program).Assembly"">
    <Found Context=""routeData"">
        <RouteView RouteData=""routeData"" />
    </Found>
</Router>
";

    /// <summary>
    /// Gets a minimal MainLayout.razor component.
    /// Required by Blazor Identity scaffolding because ManageLayout.razor inherits from the project's main layout.
    /// </summary>
    public static string GetMainLayoutRazor() =>
        @"@inherits LayoutComponentBase

<div class=""page"">
    <main>
        @Body
    </main>
</div>
";

    /// <summary>
    /// Gets a minimal NavMenu.razor component matching the standard Blazor template structure.
    /// Required by Blazor Identity scaffolding because blazorIdentityChanges.json modifies NavMenu.razor
    /// to add authentication UI (login/logout/register links).
    /// </summary>
    public static string GetNavMenuRazor() =>
        @"<div class=""top-row ps-3 navbar navbar-dark"">
    <div class=""container-fluid"">
        <a class=""navbar-brand"" href="""">TestProject</a>
    </div>
</div>

<nav class=""nav-scrollable"">
    <ul class=""nav flex-column"">
        <div class=""nav-item px-3"">
            <NavLink class=""nav-link"" href="""" Match=""NavLinkMatch.All"">
                <span class=""bi bi-house-door-fill-nav-menu"" aria-hidden=""true""></span> Home
            </NavLink>
        </div>
        <div class=""nav-item px-3"">
            <NavLink class=""nav-link"" href=""weather"">
                <span class=""bi bi-list-nested-nav-menu"" aria-hidden=""true""></span> Weather
            </NavLink>
        </div>
    </ul>
</nav>
";

    /// <summary>
    /// Gets a minimal NavMenu.razor.css matching the standard Blazor template structure.
    /// Required by Blazor Identity scaffolding because blazorIdentityChanges.json modifies NavMenu.razor.css
    /// to add CSS icons for identity navigation items.
    /// </summary>
    public static string GetNavMenuCss() =>
        @".bi-list-nested-nav-menu {
    background-image: url(""data:image/svg+xml,%3Csvg xmlns='http://www.w3.org/2000/svg' width='16' height='16' fill='white' class='bi bi-list-nested' viewBox='0 0 16 16'%3E%3Cpath fill-rule='evenodd' d='M4.5 11.5A.5.5 0 0 1 5 11h10a.5.5 0 0 1 0 1H5a.5.5 0 0 1-.5-.5zm-2-4A.5.5 0 0 1 3 7h10a.5.5 0 0 1 0 1H3a.5.5 0 0 1-.5-.5zm-2-4A.5.5 0 0 1 1 3h10a.5.5 0 0 1 0 1H1a.5.5 0 0 1-.5-.5z'/%3E%3C/svg%3E"");
}
";

    /// <summary>
    /// Sets up the standard Blazor project structure required by scaffolders that modify
    /// or reference Blazor framework files (Components/_Imports.razor, App.razor, Routes.razor,
    /// Layout/MainLayout.razor, Layout/NavMenu.razor).
    /// </summary>
    /// <param name="projectDir">The test project directory.</param>
    public static void SetupBlazorProjectStructure(string projectDir)
    {
        var componentsDir = Path.Combine(projectDir, "Components");
        Directory.CreateDirectory(componentsDir);
        File.WriteAllText(Path.Combine(componentsDir, "_Imports.razor"), GetBlazorImportsRazor());
        File.WriteAllText(Path.Combine(componentsDir, "App.razor"), GetBlazorAppRazor());
        File.WriteAllText(Path.Combine(componentsDir, "Routes.razor"), GetBlazorRoutesRazor());

        var layoutDir = Path.Combine(componentsDir, "Layout");
        Directory.CreateDirectory(layoutDir);
        File.WriteAllText(Path.Combine(layoutDir, "MainLayout.razor"), GetMainLayoutRazor());
        File.WriteAllText(Path.Combine(layoutDir, "NavMenu.razor"), GetNavMenuRazor());
        File.WriteAllText(Path.Combine(layoutDir, "NavMenu.razor.css"), GetNavMenuCss());
    }

    /// <summary>
    /// Sets up a temp project directory with a .csproj, Program.cs, and optionally a model class.
    /// Returns the project directory path.
    /// </summary>
    public static string SetupTestProject(
        string testDirectory,
        string targetFramework,
        bool includeProgram = false,
        bool includeModel = false,
        string projectName = "TestProject",
        string modelName = "TestModel")
    {
        var projectDir = Path.Combine(testDirectory, projectName);
        Directory.CreateDirectory(projectDir);

        var projectPath = Path.Combine(projectDir, $"{projectName}.csproj");
        File.WriteAllText(projectPath, GetWebProjectContent(targetFramework));

        if (includeProgram)
        {
            File.WriteAllText(Path.Combine(projectDir, "Program.cs"), GetMinimalProgramCs());
        }

        if (includeModel)
        {
            var modelsDir = Path.Combine(projectDir, "Models");
            Directory.CreateDirectory(modelsDir);
            File.WriteAllText(
                Path.Combine(modelsDir, $"{modelName}.cs"),
                GetModelClassContent(projectName, modelName));
        }

        return projectDir;
    }

    /// <summary>
    /// NuGet.config content for net11.0 preview feeds.
    /// net11.0 is in preview — the SDK and its NuGet packages live on preview-only feeds
    /// that are not in the default NuGet sources. Write this into the temp project directory
    /// so dotnet restore / build can find them.
    /// </summary>
    public static readonly string PreviewNuGetConfig = @"<?xml version=""1.0"" encoding=""utf-8""?>
<configuration>
  <packageSources>
    <clear />
    <add key=""nuget.org"" value=""https://api.nuget.org/v3/index.json"" />
    <add key=""dotnet11"" value=""https://pkgs.dev.azure.com/dnceng/public/_packaging/dotnet11/nuget/v3/index.json"" />
    <add key=""dotnet11-transport"" value=""https://pkgs.dev.azure.com/dnceng/public/_packaging/dotnet11-transport/nuget/v3/index.json"" />
    <add key=""dotnet-public"" value=""https://pkgs.dev.azure.com/dnceng/public/_packaging/dotnet-public/nuget/v3/index.json"" />
  </packageSources>
</configuration>";

    /// <summary>
    /// NuGet.config content that restricts package sources to nuget.org only.
    /// Prevents preview/dev feed packages from interfering with stable TFM tests
    /// (e.g., net8.0, net9.0) when the machine has preview SDK feeds configured.
    /// </summary>
    public static readonly string StableNuGetConfig = @"<?xml version=""1.0"" encoding=""utf-8""?>
<configuration>
  <packageSources>
    <clear />
    <add key=""nuget.org"" value=""https://api.nuget.org/v3/index.json"" />
  </packageSources>
</configuration>";
}
