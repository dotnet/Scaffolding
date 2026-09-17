// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json;
using Microsoft.DotNet.Scaffolding.Core.Scaffolders;
using Microsoft.DotNet.Scaffolding.Core.Steps;
using Microsoft.DotNet.Scaffolding.Internal.CliHelpers;
using Microsoft.DotNet.Scaffolding.Internal.Services;
using Microsoft.DotNet.Tools.Scaffold.AspNet.Common;
using Microsoft.Extensions.Logging;

namespace Microsoft.DotNet.Tools.Scaffold.AspNet.ScaffoldSteps
{
    /// <summary>
    /// Scaffold step for detecting if a referenced project is a Blazor WebAssembly project by inspecting package references.
    /// Sets context properties if a Blazor WASM project is detected.
    /// </summary>
    internal class DetectBlazorWasmStep : ScaffoldStep
    {
        /// <summary>
        /// Gets or sets the project file path to inspect.
        /// </summary>
        public string? ProjectPath { get; set; }
        private readonly ILogger _logger;
        private readonly IFileSystem _fileSystem;
        private readonly ITelemetryService _telemetryService;

        /// <summary>
        /// Initializes a new instance of the <see cref="DetectBlazorWasmStep"/> class.
        /// </summary>
        public DetectBlazorWasmStep(ILogger<DetectBlazorWasmStep> logger, IFileSystem fileSystem, ITelemetryService telemetryService)
        {
            _logger = logger;
            _fileSystem = fileSystem;
            _telemetryService = telemetryService;
        }
        
        /// <inheritdoc />
        public override Task<bool> ExecuteAsync(ScaffolderContext context, CancellationToken cancellationToken = default)
        {
            if (ProjectPath is not null)
            {
                var runner = DotnetCliRunner.CreateDotNet("reference", new[] { "list", "--project", ProjectPath });

                int exitCode = runner.ExecuteAndCaptureOutput(out var stdOut, out var stdErr);

                if (exitCode == 0 && !string.IsNullOrEmpty(stdOut))
                {
                    var references = ProjectReferenceParser.ParseProjectReferences(stdOut);
                    if (references.Count is not 0)
                    {
                        foreach (var reference in references)
                        {
                            // This step was throwing in a non-wasm (e.g. Blazor Server) project when trying to list 
                            // packages for the reference with a relative project path.
                            // Changed it to use GetReferenceFullPath to ensure the full path is correctly resolved.
                            var fullPath = GetReferenceFullPath(ProjectPath, reference);
                            if (fullPath is not null)
                            {
                                var projectRunner = DotnetCliRunner.CreateDotNet("package", new[] { "list", "--project", fullPath, "--format", "json" });
                                int packageExitCode = projectRunner.ExecuteAndCaptureOutput(out var packageStdOut, out var packageStdErr);

                                if (packageExitCode == 0 && !string.IsNullOrEmpty(packageStdOut))
                                {

                                    using (var document = JsonDocument.Parse(packageStdOut))
                                    {
                                        var root = document.RootElement;
                                        if (root.TryGetProperty("projects", out var projectsElement))
                                        {
                                            foreach (var project in projectsElement.EnumerateArray())
                                            {
                                                if (project.TryGetProperty("frameworks", out var frameworksElement))
                                                {
                                                    foreach (var framework in frameworksElement.EnumerateArray())
                                                    {
                                                        if (framework.TryGetProperty("topLevelPackages", out var topLevelPackages))
                                                        {
                                                            foreach (var pkgElement in topLevelPackages.EnumerateArray())
                                                            {
                                                                string id = pkgElement.GetProperty("id").GetString() ?? string.Empty;

                                                                if (id.StartsWith("Microsoft.NET.Sdk.WebAssembly"))
                                                                {
                                                                    context.Properties["IsBlazorWasmProject"] = true;
                                                                    context.Properties["BlazorWasmClientProjectPath"] = fullPath;
                                                                    _logger.LogInformation($"Detected Blazor WebAssembly project via package reference: {id}");
                                                                    return Task.FromResult(true);
                                                                }
                                                            }
                                                        }
                                                    }
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }
                        // Detected Blazor WebAssembly project
                        context.Properties["IsBlazorWasmProject"] = false;
                        return Task.FromResult(true);

                    }
                    else
                    {
                        context.Properties["IsBlazorWasmProject"] = false;
                        return Task.FromResult(true);

                    }
                }

            }

            return Task.FromResult(false);
        }

        /// <summary>
        /// Resolves a project reference, as printed by 'dotnet reference list' (relative to the referencing project),
        /// to a full path. The referencing project path may itself be relative to the current directory (e.g.
        /// '--project .\App\App.csproj'), so it is fully qualified first: <see cref="Path.GetFullPath(string, string)"/>
        /// throws when its base path is not fully qualified.
        /// </summary>
        /// <param name="projectPath">Path of the referencing project (absolute or relative to the current directory).</param>
        /// <param name="reference">Referenced project path as listed by 'dotnet reference list'.</param>
        /// <returns>The full path of the referenced project, or null when either input is empty.</returns>
        internal static string? GetReferenceFullPath(string? projectPath, string? reference)
        {
            if (string.IsNullOrEmpty(projectPath) || string.IsNullOrEmpty(reference))
            {
                return null;
            }

            var baseDirectory = Path.GetDirectoryName(Path.GetFullPath(projectPath));
            return string.IsNullOrEmpty(baseDirectory) ? null : Path.GetFullPath(reference, baseDirectory);
        }
    }
}
