// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Xml;
using System.Xml.Linq;
using Microsoft.DotNet.Scaffolding.Core.Scaffolders;
using Microsoft.DotNet.Scaffolding.Core.Steps;
using Microsoft.DotNet.Scaffolding.Internal.Services;
using Microsoft.DotNet.Scaffolding.Roslyn.Services;
using Microsoft.DotNet.Tools.Scaffold.AspNet.Helpers;
using Microsoft.Extensions.Logging;

namespace Microsoft.DotNet.Tools.Scaffold.AspNet.ScaffoldSteps
{
    /// <summary>
    /// Detects a Blazor WebAssembly client referenced by the selected server project.
    /// Rejects directly selected WebAssembly projects because Entra scaffolding requires a server host.
    /// </summary>
    internal class DetectBlazorWasmStep : ScaffoldStep
    {
        /// <summary>
        /// Gets or sets the project file path to inspect.
        /// </summary>
        public string? ProjectPath { get; set; }
        private readonly ILogger _logger;
        private readonly IFileSystem _fileSystem;

        /// <summary>
        /// Initializes a new instance of the <see cref="DetectBlazorWasmStep"/> class.
        /// </summary>
        public DetectBlazorWasmStep(ILogger<DetectBlazorWasmStep> logger, IFileSystem fileSystem, ITelemetryService telemetryService)
        {
            _logger = logger;
            _fileSystem = fileSystem;
        }
        
        /// <inheritdoc />
        public override Task<bool> ExecuteAsync(ScaffolderContext context, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(ProjectPath))
            {
                _logger.LogError("A valid project path is required to detect Blazor WebAssembly.");
                return Task.FromResult(false);
            }

            string fullProjectPath = Path.GetFullPath(ProjectPath);
            if (!_fileSystem.FileExists(fullProjectPath))
            {
                _logger.LogError("Project file '{ProjectPath}' does not exist.", fullProjectPath);
                return Task.FromResult(false);
            }

            try
            {
                if (UsesBlazorWebAssemblySdk(fullProjectPath))
                {
                    LogUnsupportedStandaloneProject();
                    return Task.FromResult(false);
                }

                var projectService = new MSBuildProjectService(fullProjectPath);
                if (!projectService.TryGetEvaluatedProperties(
                    ["UsingMicrosoftNETSdkBlazorWebAssembly"], out var properties, out var evaluationError))
                {
                    _logger.LogError(
                        "Unable to evaluate project '{ProjectPath}' while detecting Blazor WebAssembly. {Error} " +
                        "Ensure the project's SDK and imports are available.",
                        fullProjectPath,
                        evaluationError);
                    return Task.FromResult(false);
                }

                if (string.Equals(
                    properties["UsingMicrosoftNETSdkBlazorWebAssembly"], "true", StringComparison.OrdinalIgnoreCase))
                {
                    LogUnsupportedStandaloneProject();
                    return Task.FromResult(false);
                }

                if (!BlazorWebAssemblyClientProjectResolver.TryGetClient(
                    fullProjectPath, _fileSystem, out var client, out var error))
                {
                    _logger.LogError("{Error}", error);
                    return Task.FromResult(false);
                }

                if (client is not null)
                {
                    SetWebAssemblyProject(context, client.Value.ProjectPath);
                    return Task.FromResult(true);
                }

                context.Properties["IsBlazorWasmProject"] = false;
                return Task.FromResult(true);
            }
            catch (XmlException ex)
            {
                _logger.LogError(ex, "Unable to parse project file '{ProjectPath}' while detecting Blazor WebAssembly.", fullProjectPath);
                return Task.FromResult(false);
            }
        }

        private bool UsesBlazorWebAssemblySdk(string projectPath)
        {
            XDocument projectDocument = XDocument.Parse(_fileSystem.ReadAllText(projectPath));
            XElement? projectElement = projectDocument.Root;
            if (projectElement is null)
            {
                return false;
            }

            string? sdk = projectElement.Attribute("Sdk")?.Value;
            if (sdk?.Contains("Microsoft.NET.Sdk.BlazorWebAssembly", StringComparison.OrdinalIgnoreCase) == true)
            {
                return true;
            }

            return projectElement
                .Elements()
                .Where(element => element.Name.LocalName.Equals("Sdk", StringComparison.OrdinalIgnoreCase))
                .Select(element => element.Attribute("Name")?.Value)
                .Any(sdkName => sdkName?.Equals("Microsoft.NET.Sdk.BlazorWebAssembly", StringComparison.OrdinalIgnoreCase) == true);
        }

        private void LogUnsupportedStandaloneProject()
        {
            _logger.LogError(
                "Entra ID scaffolding requires a server host and does not support directly selected Blazor WebAssembly projects. " +
                "For a hosted Blazor Web App, select the server project instead. " +
                "Standalone WebAssembly apps require separate MSAL authentication configuration.");
        }

        private void SetWebAssemblyProject(ScaffolderContext context, string projectPath)
        {
            context.Properties["IsBlazorWasmProject"] = true;
            context.Properties["BlazorWasmClientProjectPath"] = projectPath;
            _logger.LogInformation("Detected Blazor WebAssembly project '{ProjectPath}'.", projectPath);
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
