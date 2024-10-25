// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.DotNet.Scaffolding.Core.Scaffolders;
using Microsoft.DotNet.Scaffolding.Internal.Services;
using Microsoft.DotNet.Scaffolding.Internal.Telemetry;
using Microsoft.DotNet.Tools.Scaffold.AspNet.Telemetry;
using Microsoft.Extensions.Logging;
using TelemetryConstants = Microsoft.DotNet.Tools.Scaffold.AspNet.Telemetry.TelemetryConstants;
namespace Microsoft.DotNet.Scaffolding.Core.Steps;

internal class AddConnectionStringStep : ScaffoldStep
{
    public required string BaseProjectPath { get; set; }
    public required string ConnectionStringName { get; set; }
    public required string ConnectionString { get; set; }
    private readonly ILogger _logger;
    private readonly IFileSystem _fileSystem;
    private readonly ITelemetryService _telemetryService;
    public AddConnectionStringStep(
        ILogger<AddConnectionStringStep> logger,
        IFileSystem fileSystem,
        ITelemetryService telemetryService)
    {
        _logger = logger;
        _fileSystem = fileSystem;
        _telemetryService = telemetryService;
    }

    public override Task<bool> ExecuteAsync(ScaffolderContext context, CancellationToken cancellationToken = default)
    {
        var appSettingsFileSearch = _fileSystem.EnumerateFiles(BaseProjectPath, "appsettings.json", SearchOption.AllDirectories);
        var appSettingsFile = appSettingsFileSearch.FirstOrDefault();
        JsonNode? content;
        bool writeContent = false;
        if (string.IsNullOrEmpty(appSettingsFile) || !_fileSystem.FileExists(appSettingsFile))
        {
            content = new JsonObject();
            writeContent = true;
        }
        else
        {
            var jsonString = _fileSystem.ReadAllText(appSettingsFile);
            content = JsonNode.Parse(jsonString);
        }

        if (content is null)
        {
            _logger.LogError($"Failed to parse appsettings.json file at {appSettingsFile}");
            _telemetryService.TrackEvent(new AddConnectionStringTelemetryEvent(context.Scaffolder.DisplayName, TelemetryConstants.Failure, "Failed to parse appsettings.json"));
            return Task.FromResult(false);
        }

        string connectionStringNodeName = "ConnectionStrings";

        //find the "ConnectionStrings" node.
        if (content[connectionStringNodeName] is null)
        {
            writeContent = true;
            content[connectionStringNodeName] = new JsonObject();
        }

        //if a key with the 'databaseName' already exists, skipping adding a connection string.
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
            var options = new JsonSerializerOptions { WriteIndented = true };
            _fileSystem.WriteAllText(appSettingsFile, content.ToJsonString(options));
            _logger.LogInformation($"Updated '{Path.GetFileName(appSettingsFile)}' with connection string '{ConnectionStringName}'");
            _telemetryService.TrackEvent(new AddConnectionStringTelemetryEvent(context.Scaffolder.Name, TelemetryConstants.Added));
        }
        else
        {
            _telemetryService.TrackEvent(new AddConnectionStringTelemetryEvent(context.Scaffolder.Name, TelemetryConstants.NoChange));
        }

        return Task.FromResult(true);
    }
}
