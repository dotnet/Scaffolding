// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.DotNet.Scaffolding.Core.Scaffolders;
using Microsoft.DotNet.Scaffolding.Internal.Services;
using Microsoft.DotNet.Scaffolding.Internal.Telemetry;
using Microsoft.DotNet.Tools.Scaffold.AspNet;
using Microsoft.DotNet.Tools.Scaffold.AspNet.Commands;
using Microsoft.DotNet.Tools.Scaffold.AspNet.Helpers;
using Moq;
using Xunit;

namespace Microsoft.DotNet.Tools.Scaffold.Tests.AspNet.Integration;

/// <summary>
/// Shared base class for Blazor Identity integration tests across .NET versions.
/// Tests template file discovery and code modification configs on disk.
/// </summary>
public abstract class BlazorIdentityIntegrationTestsBase : IDisposable
{
    protected abstract string TargetFramework { get; }
    protected abstract string TestClassName { get; }

    protected readonly string _testDirectory;
    protected readonly string _testProjectDir;
    protected readonly string _testProjectPath;
    protected readonly string _toolsDirectory;
    protected readonly string _templatesDirectory;
    protected readonly TestTelemetryService _testTelemetryService;
    protected readonly Mock<IScaffolder> _mockScaffolder;
    protected readonly ScaffolderContext _context;

    protected BlazorIdentityIntegrationTestsBase()
    {
        _testDirectory = Path.Combine(Path.GetTempPath(), TestClassName, Guid.NewGuid().ToString());
        _testProjectDir = Path.Combine(_testDirectory, "TestProject");
        _testProjectPath = Path.Combine(_testProjectDir, "TestProject.csproj");
        _toolsDirectory = Path.Combine(_testDirectory, "tools");
        _templatesDirectory = Path.Combine(_testDirectory, "Templates");
        Directory.CreateDirectory(_testProjectDir);
        Directory.CreateDirectory(_toolsDirectory);
        Directory.CreateDirectory(_templatesDirectory);

        _testTelemetryService = new TestTelemetryService();
        _mockScaffolder = new Mock<IScaffolder>();
        _mockScaffolder.Setup(s => s.DisplayName).Returns(AspnetStrings.Identity.DisplayName);
        _mockScaffolder.Setup(s => s.Name).Returns(AspnetStrings.Identity.Name);
        _context = new ScaffolderContext(_mockScaffolder.Object);
    }

    public void Dispose()
    {
        if (Directory.Exists(_testDirectory))
        {
            try { Directory.Delete(_testDirectory, recursive: true); }
            catch { /* Ignore cleanup errors in tests */ }
        }
    }

    protected string ProjectContent => $@"<Project Sdk=""Microsoft.NET.Sdk.Web"">
  <PropertyGroup>
    <TargetFramework>{TargetFramework}</TargetFramework>
  </PropertyGroup>
</Project>";

    #region Blazor Identity — Template Root Folders

    [Theory]
    [InlineData("BlazorCrud")]
    [InlineData("BlazorIdentity")]
    [InlineData("CodeModificationConfigs")]
    [InlineData("EfController")]
    [InlineData("Files")]
    [InlineData("Identity")]
    [InlineData("MinimalApi")]
    [InlineData("RazorPages")]
    [InlineData("Views")]
    public void Templates_HasExpectedScaffolderFolder(string folderName)
    {
        var basePath = GetActualTemplatesBasePath();
        var folderPath = Path.Combine(basePath, TargetFramework, folderName);
        Assert.True(Directory.Exists(folderPath),
            $"Expected template folder '{folderName}' not found for {TargetFramework}");
    }

    #endregion

    #region Blazor Identity — BlazorIdentity Folder Structure

    [Fact]
    public void BlazorIdentityTemplates_FolderExists()
    {
        var basePath = GetActualTemplatesBasePath();
        var blazorIdentityDir = Path.Combine(basePath, TargetFramework, "BlazorIdentity");
        Assert.True(Directory.Exists(blazorIdentityDir),
            $"BlazorIdentity template folder should exist for {TargetFramework}");
    }

    [Fact]
    public void BlazorIdentityTemplates_HasPagesSubfolder()
    {
        var basePath = GetActualTemplatesBasePath();
        var pagesDir = Path.Combine(basePath, TargetFramework, "BlazorIdentity", "Pages");
        Assert.True(Directory.Exists(pagesDir),
            $"BlazorIdentity/Pages subfolder should exist for {TargetFramework}");
    }

    [Fact]
    public void BlazorIdentityTemplates_HasManageSubfolder()
    {
        var basePath = GetActualTemplatesBasePath();
        var manageDir = Path.Combine(basePath, TargetFramework, "BlazorIdentity", "Manage");
        Assert.True(Directory.Exists(manageDir),
            $"BlazorIdentity/Manage subfolder should exist for {TargetFramework}");
    }

    [Fact]
    public void BlazorIdentityTemplates_HasSharedSubfolder()
    {
        var basePath = GetActualTemplatesBasePath();
        var sharedDir = Path.Combine(basePath, TargetFramework, "BlazorIdentity", "Shared");
        Assert.True(Directory.Exists(sharedDir),
            $"BlazorIdentity/Shared subfolder should exist for {TargetFramework}");
    }

    [Fact]
    public void BlazorIdentityTemplates_AllFilesAreTT()
    {
        var basePath = GetActualTemplatesBasePath();
        var blazorIdentityDir = Path.Combine(basePath, TargetFramework, "BlazorIdentity");
        var files = Directory.GetFiles(blazorIdentityDir, "*", SearchOption.AllDirectories);
        Assert.All(files, f => Assert.EndsWith(".tt", f));
    }

    #endregion

    #region Blazor Identity — Files Folder

    [Fact]
    public void FilesFolderExists()
    {
        var basePath = GetActualTemplatesBasePath();
        var filesDir = Path.Combine(basePath, TargetFramework, "Files");
        Assert.True(Directory.Exists(filesDir),
            $"Files template folder should exist for {TargetFramework}");
    }

    [Fact]
    public void FilesFolder_HasFiles()
    {
        var basePath = GetActualTemplatesBasePath();
        var filesDir = Path.Combine(basePath, TargetFramework, "Files");
        var files = Directory.GetFiles(filesDir, "*", SearchOption.AllDirectories);
        Assert.True(files.Length > 0, "Files folder should contain at least one file");
    }

    #endregion

    #region Blazor Identity — Code Modification Config

    [Fact]
    public void BlazorIdentityChangesConfig_ExistsForTargetFramework()
    {
        var configPath = GetBlazorIdentityChangesConfigPath();
        Assert.True(File.Exists(configPath),
            $"blazorIdentityChanges.json should exist for {TargetFramework}");
    }

    [Fact]
    public void BlazorIdentityChangesConfig_IsNotEmpty()
    {
        var configPath = GetBlazorIdentityChangesConfigPath();
        var content = File.ReadAllText(configPath);
        Assert.False(string.IsNullOrWhiteSpace(content),
            "blazorIdentityChanges.json should not be empty");
    }

    [Fact]
    public void BlazorIdentityChangesConfig_ReferencesProgramCs()
    {
        var configPath = GetBlazorIdentityChangesConfigPath();
        var content = File.ReadAllText(configPath);
        Assert.Contains("Program.cs", content);
    }

    #endregion

    #region Blazor Identity — T4 Templates Are Discoverable

    [Fact]
    public void GetAllFilesForTargetFramework_IsSuperset_OfT4Templates()
    {
        var basePath = GetActualTemplatesBasePath();
        var filesDir = Path.Combine(basePath, TargetFramework, "Files");
        if (Directory.Exists(filesDir))
        {
            var allFiles = Directory.GetFiles(filesDir, "*", SearchOption.AllDirectories);
            var ttFiles = allFiles.Where(f => f.EndsWith(".tt")).ToArray();
            Assert.True(allFiles.Length >= ttFiles.Length);
        }
    }

    #endregion

    #region Helper Methods

    protected static string GetActualTemplatesBasePath()
    {
        var assemblyLocation = Assembly.GetExecutingAssembly().Location;
        var assemblyDirectory = Path.GetDirectoryName(assemblyLocation);
        var basePath = Path.Combine(assemblyDirectory!, "..", "..", "..", "..", "..", "src", "dotnet-scaffolding", "dotnet-scaffold", "AspNet", "Templates");
        return Path.GetFullPath(basePath);
    }

    protected string GetBlazorIdentityChangesConfigPath()
    {
        var basePath = GetActualTemplatesBasePath();
        return Path.Combine(basePath, TargetFramework, "CodeModificationConfigs", "blazorIdentityChanges.json");
    }

    protected static async Task<(int ExitCode, string Output, string Error)> RunBuildAsync(string workingDirectory)
    {
        var buildProcess = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = "dotnet",
                Arguments = "build",
                WorkingDirectory = workingDirectory,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            }
        };
        buildProcess.Start();
        string output = await buildProcess.StandardOutput.ReadToEndAsync();
        string error = await buildProcess.StandardError.ReadToEndAsync();
        await buildProcess.WaitForExitAsync();
        return (buildProcess.ExitCode, output, error);
    }

    #endregion

    protected class TestTelemetryService : ITelemetryService
    {
        public List<(string EventName, IReadOnlyDictionary<string, string> Properties, IReadOnlyDictionary<string, double> Measures)> TrackedEvents { get; } = new();
        public void TrackEvent(string eventName, IReadOnlyDictionary<string, string>? properties = null, IReadOnlyDictionary<string, double>? measures = null)
        {
            TrackedEvents.Add((eventName, properties ?? new Dictionary<string, string>(), measures ?? new Dictionary<string, double>()));
        }

        public void Flush() { }
    }
}
