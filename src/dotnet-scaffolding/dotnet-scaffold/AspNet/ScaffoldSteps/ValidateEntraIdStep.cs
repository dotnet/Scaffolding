// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.DotNet.Scaffolding.Core.Scaffolders;
using Microsoft.DotNet.Scaffolding.Core.Steps;
using Microsoft.DotNet.Scaffolding.Internal.Services;
using Microsoft.DotNet.Scaffolding.Internal.Telemetry;
using Microsoft.DotNet.Tools.Scaffold.AspNet.Common;
using Microsoft.DotNet.Tools.Scaffold.AspNet.Models;
using Microsoft.DotNet.Tools.Scaffold.AspNet.ScaffoldSteps.Settings;
using Microsoft.DotNet.Tools.Scaffold.AspNet.Telemetry;
using Microsoft.Extensions.Logging;
using AspNetConstants = Microsoft.DotNet.Tools.Scaffold.AspNet.Common.Constants;
using Constants = Microsoft.DotNet.Scaffolding.Internal.Constants;

namespace Microsoft.DotNet.Tools.Scaffold.AspNet.ScaffoldSteps
{
    /// <summary>
    /// Scaffold step to validate Entra ID settings and initialize the EntraIdModel for scaffolding.
    /// </summary>
    internal class ValidateEntraIdStep : ScaffoldStep
    {
        private readonly IFileSystem _fileSystem;
        private readonly ILogger _logger;
        private readonly ITelemetryService _telemetryService;

        // Properties as requested
        /// <summary>
        /// Username for Entra ID configuration.
        /// </summary>
        public string? Username { get; set; }
        /// <summary>
        /// Path to the project file.
        /// </summary>
        public string? Project { get; set; }
        /// <summary>
        /// Tenant ID for Entra ID configuration.
        /// </summary>
        public string? TenantId { get; set; }
        /// <summary>
        /// Application name for Entra ID configuration.
        /// </summary>
        public string? Application { get; set; }
        /// <summary>
        /// Option to select application interactively.
        /// </summary>
        public string? SelectApplication { get; set; }

        /// <summary>
        /// Constructor for ValidateEntraIdStep.
        /// </summary>
        /// <param name="fileSystem">File system service.</param>
        /// <param name="logger">Logger service.</param>
        /// <param name="telemetryService">Telemetry service.</param>
        public ValidateEntraIdStep(
            IFileSystem fileSystem,
            ILogger<ValidateEntraIdStep> logger,
            ITelemetryService telemetryService)
        {
            _fileSystem = fileSystem;
            _logger = logger;
            _telemetryService = telemetryService;
        }

        /// <summary>
        /// Executes the step to validate Entra ID settings and initialize the EntraIdModel.
        /// </summary>
        public override async Task<bool> ExecuteAsync(ScaffolderContext context, CancellationToken cancellationToken = default)
        {
            var entraIdSettings = ValidateEntraIdSettings();
            var codeModifierProperties = new Dictionary<string, string>();

            if (entraIdSettings is null)
            {
                _telemetryService.TrackEvent(new ValidateScaffolderTelemetryEvent(nameof(ValidateEntraIdStep), context.Scaffolder.DisplayName, false));
                return false;
            }
            else
            {
                context.Properties.Add(nameof(EntraIdSettings), entraIdSettings);
            }

            _logger.LogInformation("Initializing Entra ID scaffolding model...");
            var entraIdModel = await GetEntraIdModelAsync(context, entraIdSettings);

            if (entraIdModel is null)
            {
                _logger.LogError("An error occurred while initializing Entra ID model.");
                _telemetryService.TrackEvent(new ValidateScaffolderTelemetryEvent(nameof(ValidateEntraIdStep), context.Scaffolder.DisplayName, false));
                return false;
            }
            else
            {
                context.Properties.Add(nameof(EntraIdModel), entraIdModel);

                // Add relevant properties to the codeModifier, ensuring non-null values
                if (!string.IsNullOrEmpty(entraIdModel.Username))
                {
                    codeModifierProperties.Add("EntraIdUsername", entraIdModel.Username);
                }

                if (!string.IsNullOrEmpty(entraIdModel.TenantId))
                {
                    codeModifierProperties.Add("EntraIdTenantId", entraIdModel.TenantId);
                }

                if (!string.IsNullOrEmpty(entraIdModel.Application))
                {
                    codeModifierProperties.Add("EntraIdApplication", entraIdModel.Application);
                }
            }

            context.Properties.Add(Constants.StepConstants.CodeModifierProperties, codeModifierProperties);
            _telemetryService.TrackEvent(new ValidateScaffolderTelemetryEvent(nameof(ValidateEntraIdStep), context.Scaffolder.DisplayName, true));
            return true;
        }

        /// <summary>
        /// Validates the Entra ID settings provided by the user.
        /// </summary>
        private EntraIdSettings? ValidateEntraIdSettings()
        {
            if (string.IsNullOrEmpty(Project) || !_fileSystem.FileExists(Project))
            {
                _logger.LogError($"Missing/Invalid {AspNetConstants.CliOptions.ProjectCliOption} option.");
                return null;
            }

            if (string.IsNullOrEmpty(Username))
            {
                _logger.LogError("Missing/Invalid Username option.");
                return null;
            }

            if (string.IsNullOrEmpty(TenantId))
            {
                _logger.LogError("Missing/Invalid TenantId option.");
                return null;
            }

            if (string.IsNullOrEmpty(Application) && string.IsNullOrEmpty(SelectApplication))
            {
                _logger.LogError("Either Application must be specified or SelectApplication must be true.");
                return null;
            }

            return new EntraIdSettings
            {
                Username = Username,
                Project = Project,
                TenantId = TenantId,
                Application = Application,
                SelectApplication = SelectApplication
            };
        }

        /// <summary>
        /// Initializes and returns the EntraIdModel for scaffolding.
        /// </summary>
        private async Task<EntraIdModel?> GetEntraIdModelAsync(ScaffolderContext context, EntraIdSettings settings)
        {
            if (string.IsNullOrEmpty(settings.Project))
            {
                _logger.LogError("Project path is null or empty.");
                return null;
            }

            ProjectInfo projectInfo = ClassAnalyzers.GetProjectInfo(settings.Project, _logger);
            context.SetSpecifiedTargetFramework(projectInfo.LowestTargetFramework);
            var projectDirectory = Path.GetDirectoryName(projectInfo?.ProjectPath);

            if (projectInfo is null || projectInfo.CodeService is null || string.IsNullOrEmpty(projectDirectory))
            {
                return null;
            }

            var projectName = Path.GetFileNameWithoutExtension(settings.Project);
            if (string.IsNullOrEmpty(projectName))
            {
                return null;
            }

            EntraIdModel scaffoldingModel = new()
            {
                ProjectInfo = projectInfo,
                Username = settings.Username,
                TenantId = settings.TenantId,
                Application = settings.Application,
                SelectApplication = settings.SelectApplication,
                BaseOutputPath = projectDirectory,
                EntraIdNamespace = $"{projectName}"
            };

            return await Task.FromResult(scaffoldingModel);
        }
    }
}
