// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.DotNet.Scaffolding.Core.Scaffolders;
using Microsoft.DotNet.Scaffolding.Core.Steps;
using Microsoft.DotNet.Scaffolding.Internal.CliHelpers;
using Microsoft.DotNet.Scaffolding.Internal.Services;
using Microsoft.DotNet.Tools.Scaffold.Helpers;
using Microsoft.Extensions.Logging;
using System.IO;
using System.Text.RegularExpressions;

namespace Microsoft.DotNet.Tools.Scaffold.AspNet.ScaffoldSteps
{
    /// <summary>
    /// Scaffold step for registering or updating an Azure AD application using the msidentity CLI.
    /// Captures and stores the client ID in the scaffolding context.
    /// </summary>
    internal class RegisterAppStep : ScaffoldStep
    {
        /// <summary>
        /// Gets or sets the project file path.
        /// </summary>
        public required string ProjectPath { get; set; }
        /// <summary>
        /// Gets or sets the username for Azure AD.
        /// </summary>
        public string? Username { get; set; }
        /// <summary>
        /// Gets or sets the Azure AD tenant ID.
        /// </summary>
        public string? TenantId { get; set; }
        /// <summary>
        /// Gets or sets the Azure AD client ID (if updating an existing app).
        /// </summary>
        public string? ClientId { get; set; }

        private readonly ILogger _logger;
        private readonly IFileSystem _fileSystem;
        private readonly ITelemetryService _telemetryService;

        /// <summary>
        /// Initializes a new instance of the <see cref="RegisterAppStep"/> class.
        /// </summary>
        public RegisterAppStep(
            ILogger<AddClientSecretStep> logger,
            IFileSystem fileSystem,
            ITelemetryService telemetryService)
        {
            _logger = logger;
            _fileSystem = fileSystem;
            _telemetryService = telemetryService;
        }

        /// <inheritdoc />
        public override Task<bool> ExecuteAsync(ScaffolderContext context, CancellationToken cancellationToken = default)
        {
            // Validate project path
            if (string.IsNullOrEmpty(ProjectPath) || !_fileSystem.FileExists(ProjectPath))
            {
                _logger.LogError($"Invalid project path: {ProjectPath}");
                return Task.FromResult(false);
            }

            // Ensure the msidentity tool is available before attempting to invoke it,
            // so a missing tool surfaces a clear, actionable message instead of a cryptic dotnet error.
            if (!EnsureMsIdentityIsInstalled())
            {
                return Task.FromResult(false);
            }

            if(ClientId is not null)
            {
                _logger.LogInformation("Updating project...");
                // Initialize user secrets
                bool success = RegisterApp(context);
                if (!success)
                {
                    _logger.LogError("Failed to Update App.");
                    return Task.FromResult(false);
                }
                _logger.LogInformation("Successfully Updated App.");
                return Task.FromResult(true);
            }
            else
            {
                _logger.LogInformation("Registering an application for local development and testing. Use a separate app registration for production.");
                // Initialize user secrets
                bool success = RegisterApp(context);
                if (!success)
                {
                    _logger.LogError("Failed to Register App.");
                    return Task.FromResult(false);
                }
                _logger.LogInformation("Successfully Registered App.");
                return Task.FromResult(true);
            }
        }

        /// <summary>
        /// Registers a new Azure AD application and captures the client ID.
        /// </summary>
        private bool RegisterApp(ScaffolderContext context)
        {
            try
            {
                // Fix for CS8620: Ensure all elements in the array are non-null by using null-coalescing operator
                var args = new List<string>
                {
                    "--register-app",
                    "--registration-only",
                    "--project-file-path", Path.GetFullPath(ProjectPath),
                    "--project-type", "webapp",
                    "--app-display-name", Path.GetFileNameWithoutExtension(ProjectPath),
                    "--tenant-id", TenantId ?? string.Empty,
                    "--username", Username ?? string.Empty
                };
                if (!string.IsNullOrEmpty(ClientId))
                {
                    args.AddRange(["--client-id", ClientId]);
                }

                int exitCode = ExecuteMsIdentity(args.ToArray(), out var stdOut, out var stdErr);
                if (exitCode != 0)
                {
                    _logger.LogError($"Error registering application: {stdErr}");
                    _logger.LogError(MsIdentityToolHelper.NotInstalledMessage);
                    return false;
                }
                if (!string.IsNullOrEmpty(stdOut))
                {
                    // Extract client ID using regex
                    var match = Regex.Match(stdOut, @"(?m)^ClientId: ([0-9a-fA-F-]+)\s*$");
                    if (match.Success && Guid.TryParse(match.Groups[1].Value, out Guid clientId))
                    {
                        ClientId = clientId.ToString("D");
                        context.Properties["ClientId"] = ClientId;
                        _logger.LogInformation($"Captured client ID: {ClientId}");
                    }
                    else
                    {
                        _logger.LogWarning("Could not extract client ID from output");
                        return false;
                    }
                }
                else
                {
                    _logger.LogError("Registration returned no client ID.");
                    return false;
                }
                return true;
            }
            catch (Exception e)
            {
                _logger.LogError($"Error Registering App: {e.Message}");
                return false;
            }
        }

        protected virtual bool EnsureMsIdentityIsInstalled() => MsIdentityToolHelper.EnsureMsIdentityIsInstalled(_logger);

        protected virtual int ExecuteMsIdentity(string[] args, out string? stdOut, out string? stdErr) =>
            DotnetCliRunner.CreateDotNet("msidentity", args).ExecuteAndCaptureOutput(out stdOut, out stdErr);
    }
}
