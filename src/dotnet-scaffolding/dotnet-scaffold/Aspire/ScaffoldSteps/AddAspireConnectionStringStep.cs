// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.DotNet.Scaffolding.Core.Steps;
using Microsoft.DotNet.Scaffolding.Core.Scaffolders;
using Microsoft.DotNet.Scaffolding.Internal.Services;
using Microsoft.DotNet.Scaffolding.Internal.Telemetry;
using Microsoft.DotNet.Tools.Scaffold.Aspire.Telemetry;
using Microsoft.Extensions.Logging;
using TelemetryConstants = Microsoft.DotNet.Tools.Scaffold.Aspire.Telemetry.TelemetryConstants;

namespace Microsoft.DotNet.Tools.Scaffold.Aspire.ScaffoldSteps;

/// <summary>
/// A scaffold step that adds a connection string to the appsettings.json file of a project.
/// </summary>
internal class AddAspireConnectionStringStep : ScaffoldStep
{
    public required string BaseProjectPath { get; set; }
    public required string ConnectionStringName { get; set; }
    public required string ConnectionString { get; set; }
    private readonly ILogger _logger;
    private readonly IFileSystem _fileSystem;
    private readonly ITelemetryService _telemetryService;

    /// <summary>
    /// Initializes a new instance of the <see cref="AddAspireConnectionStringStep"/> class.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="fileSystem">The file system abstraction.</param>
    /// <param name="telemetryService">The telemetry service instance.</param>
    public AddAspireConnectionStringStep(
        ILogger<AddAspireConnectionStringStep> logger,
        IFileSystem fileSystem,
        ITelemetryService telemetryService)
    {
        _logger = logger;
        _fileSystem = fileSystem;
        _telemetryService = telemetryService;
    }

    /// <summary>
    /// Executes the step to add a connection string to appsettings.json, creating the file if necessary.
    /// </summary>
    /// <param name="context">The scaffolder context.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>True if the connection string was added or already present; otherwise, false.</returns>
    public override Task<bool> ExecuteAsync(ScaffolderContext context, CancellationToken cancellationToken = default)
    {
        IEnumerable<string> appSettingsFileSearch = _fileSystem.EnumerateFiles(BaseProjectPath, "appsettings.json", SearchOption.AllDirectories);
        string? appSettingsFile = appSettingsFileSearch.FirstOrDefault();
        JsonNode? content;
        bool writeContent = false;
        if (string.IsNullOrEmpty(appSettingsFile) || !_fileSystem.FileExists(appSettingsFile))
        {
            // Create a new appsettings.json if it doesn't exist
            appSettingsFile = Path.Combine(BaseProjectPath, "appsettings.json");
            content = new JsonObject();
            writeContent = true;
        }
        else
        {
            // Parse the existing appsettings.json
            string jsonString = _fileSystem.ReadAllText(appSettingsFile);
            content = JsonNode.Parse(jsonString);
        }

        if (content is null)
        {
            _logger.LogError($"Failed to parse appsettings.json file at {appSettingsFile}");
            _telemetryService.TrackEvent(new AddAspireConnectionStringTelemetryEvent(context.Scaffolder.DisplayName, TelemetryConstants.Failure, "Failed to parse appsettings.json"));
            return Task.FromResult(false);
        }

        string connectionStringNodeName = "ConnectionStrings";

        // Find or create the "ConnectionStrings" node
        if (content[connectionStringNodeName] is null)
        {
            writeContent = true;
            content[connectionStringNodeName] = new JsonObject();
        }

        // If a key with the 'databaseName' does not exist, add the connection string
        if (content[connectionStringNodeName] is JsonObject connectionStringObject &&
            connectionStringObject[ConnectionStringName] is null &&
            !string.IsNullOrEmpty(ConnectionString))
        {
            writeContent = true;
            connectionStringObject[ConnectionStringName] = ConnectionString;
            content[connectionStringNodeName] = connectionStringObject;
        }

        if (writeContent && !string.IsNullOrEmpty(appSettingsFile))
        {
            // Write the updated content to appsettings.json
            JsonSerializerOptions options = new JsonSerializerOptions { WriteIndented = true };
            _fileSystem.WriteAllText(appSettingsFile, content.ToJsonString(options));
            _logger.LogInformation($"Updated '{Path.GetFileName(appSettingsFile)}' with connection string '{ConnectionStringName}'");
            _telemetryService.TrackEvent(new AddAspireConnectionStringTelemetryEvent(context.Scaffolder.Name, TelemetryConstants.Added));
        }
        else
        {
            _telemetryService.TrackEvent(new AddAspireConnectionStringTelemetryEvent(context.Scaffolder.Name, TelemetryConstants.NoChange));
        }

        return Task.FromResult(true);
    }
}
