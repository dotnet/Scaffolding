// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.DotNet.Scaffolding.Core.Scaffolders;
using Microsoft.DotNet.Scaffolding.Core.Steps;
using Microsoft.DotNet.Scaffolding.Internal.CliHelpers;
using Microsoft.DotNet.Scaffolding.Internal.Services;
using Microsoft.DotNet.Tools.Scaffold.Helpers;
using Microsoft.DotNet.Tools.Scaffold.AspNet.Helpers;
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
            if (!EnsureToolInstalled())
            {
                return Task.FromResult(false);
            }

            if (ClientId is null && !MsIdentityToolHelper.SupportsRegistrationCleanup(_logger, ReadCapabilities))
            {
                return Task.FromResult(false);
            }

            if(ClientId is not null)
            {
                _logger.LogInformation("Updating project...");
                // Initialize user secrets
                bool success = UpdateApp(context);
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
        protected virtual bool RegisterApp(ScaffolderContext context)
        {
            try
            {
                // Fix for CS8620: Ensure all elements in the array are non-null by using null-coalescing operator
                var args = new[]
                {
                    "--create-app-registration",
                    "--tenant-id", TenantId ?? string.Empty,
                    "--username", Username ?? string.Empty
                };
                // Fix for IDE0300: Simplify collection initialization
                var (exitCode, stdOut, stdErr) = RunRegistration(args);
                if (exitCode != 0)
                {
                    _logger.LogError($"Error registering application: {stdErr}");
                    _logger.LogError(MsIdentityToolHelper.NotInstalledMessage);
                    return false;
                }
                if (!string.IsNullOrEmpty(stdOut))
                {
                    // Extract client ID using regex
                    var match = Regex.Match(stdOut, @"Created app .+ - ([0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12})");
                    if (match.Success && match.Groups.Count > 1)
                    {
                        ClientId = match.Groups[1].Value;
                        context.Properties["ClientId"] = ClientId;
                        context.Properties[EntraRegistrationCleanup.CreatedClientIdKey] = ClientId;
                        _logger.LogInformation($"Captured client ID: {ClientId}");
                    }
                    else
                    {
                        _logger.LogError("Could not extract the created client ID. Check the selected tenant for an application created during this run and remove it before retrying.");
                        return false;
                    }
                }
                if (string.IsNullOrEmpty(ClientId))
                {
                    _logger.LogError("Registration returned no client ID. Check the selected tenant for an application created during this run before retrying.");
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

        protected virtual bool EnsureToolInstalled()
            => MsIdentityToolHelper.EnsureMsIdentityIsInstalled(_logger);

        protected virtual (int ExitCode, string? Output, string? Error) RunRegistration(string[] args)
        {
            var runner = DotnetCliRunner.CreateDotNet("msidentity", args);
            var exitCode = runner.ExecuteAndCaptureOutput(out var output, out var error);
            return (exitCode, output, error);
        }

        protected virtual (int ExitCode, string? Output, string? Error) ReadCapabilities()
            => MsIdentityToolHelper.ReadCapabilities();

        /// <summary>
        /// Updates an existing Azure AD application registration.
        /// </summary>
        private bool UpdateApp(ScaffolderContext context)
        {
            try
            {
                // Fix for CS8620: Ensure all elements in the array are non-null by using null-coalescing operator
                var args = new[]
                {
                    "--update-app-registration",
                    "--client-id", ClientId ?? string.Empty,
                    "--tenant-id", TenantId ?? string.Empty,
                    "--username", Username ?? string.Empty,
                };
                // Fix for IDE0300: Simplify collection initialization
                var runner = DotnetCliRunner.CreateDotNet("msidentity", args);
                int exitCode = runner.ExecuteAndCaptureOutput(out var stdOut, out var stdErr);
                if (exitCode != 0)
                {
                    _logger.LogError($"Error updating registration: {stdErr}");
                    _logger.LogError(MsIdentityToolHelper.NotInstalledMessage);
                    return false;
                }
                if (!string.IsNullOrEmpty(stdOut))
                {
                    // Extract client ID using regex
                    var match = Regex.Match(stdOut, @"Created app .+ - ([0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12})");
                    if (match.Success && match.Groups.Count > 1)
                    {
                        ClientId = match.Groups[1].Value;
                        context.Properties["ClientId"] = ClientId;
                        _logger.LogInformation($"Captured client ID: {ClientId}");
                    }
                    else
                    {
                        _logger.LogWarning("Could not extract client ID from output");
                    }
                }
                return true;
            }
            catch (Exception e)
            {
                _logger.LogError($"Error Registering App: {e.Message}");
                return false;
            }
        }
    }
}
