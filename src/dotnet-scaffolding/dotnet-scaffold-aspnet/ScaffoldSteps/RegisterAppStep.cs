// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.DotNet.Scaffolding.Core.Scaffolders;
using Microsoft.DotNet.Scaffolding.Core.Steps;
using Microsoft.DotNet.Scaffolding.Internal.CliHelpers;
using Microsoft.DotNet.Scaffolding.Internal.Services;
using Microsoft.DotNet.Scaffolding.Internal.Telemetry;
using Microsoft.Extensions.Logging;
using System.IO;
using System.Text.RegularExpressions;

namespace Microsoft.DotNet.Tools.Scaffold.AspNet.ScaffoldSteps
{
    internal class RegisterAppStep : ScaffoldStep
    {
        // Required properties
        public required string ProjectPath { get; set; }
        public string? Username { get; set; }
        public string? TenantId { get; set; }
        public string? ClientId { get; set; }

        private readonly ILogger _logger;
        private readonly IFileSystem _fileSystem;
        private readonly ITelemetryService _telemetryService;

        public RegisterAppStep(
            ILogger<AddClientSecretStep> logger,
            IFileSystem fileSystem,
            ITelemetryService telemetryService)
        {
            _logger = logger;
            _fileSystem = fileSystem;
            _telemetryService = telemetryService;
        }

        public override Task<bool> ExecuteAsync(ScaffolderContext context, CancellationToken cancellationToken = default)
        {
            // Validate project path
            if (string.IsNullOrEmpty(ProjectPath) || !_fileSystem.FileExists(ProjectPath))
            {
                _logger.LogError($"Invalid project path: {ProjectPath}");
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
                _logger.LogInformation("Registering project...");

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

        private bool RegisterApp(ScaffolderContext context)
        {
            try
            {
                // Fix for CS8620: Ensure all elements in the array are non-null by using null-coalescing operator
                var args = new[]
                {
                    "--register-app",
                    "--tenant-id", TenantId ?? string.Empty,
                    "--username", Username ?? string.Empty
                };

                // Fix for IDE0300: Simplify collection initialization
                var runner = DotnetCliRunner.CreateDotNet("msidentity", args);

                int exitCode = runner.ExecuteAndCaptureOutput(out var stdOut, out var stdErr);

                if (exitCode != 0)
                {
                    _logger.LogError($"Error registering application: {stdErr}");
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
