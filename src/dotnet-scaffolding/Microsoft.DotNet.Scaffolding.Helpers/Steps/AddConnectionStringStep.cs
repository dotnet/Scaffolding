// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.DotNet.Scaffolding.Core.Scaffolders;
using Microsoft.Extensions.Logging;
using ScaffoldingStep = Microsoft.DotNet.Scaffolding.Core.Steps.ScaffoldStep;

namespace Microsoft.DotNet.Scaffolding.Helpers.Steps;

internal class AddConnectionStringStep : ScaffoldingStep
{
    public required string BaseProjectPath { get; set; }
    public required string ConnectionStringName { get; set; }
    public required string ConnectionString { get; set; }
    private readonly ILogger _logger;

    public AddConnectionStringStep(ILogger<AddConnectionStringStep> logger)
    {
        _logger = logger;
    }

    public override async Task ExecuteAsync(ScaffolderContext context, CancellationToken cancellationToken = default)
    {
        var appSettingsFileSearch = Directory.EnumerateFiles(BaseProjectPath, "appsettings.json", SearchOption.AllDirectories);
        var appSettingsFile = appSettingsFileSearch.FirstOrDefault();
        JsonNode? content;
        bool writeContent = false;

        if (string.IsNullOrEmpty(appSettingsFile) || !File.Exists(appSettingsFile))
        {
            content = new JsonObject();
            writeContent = true;
        }
        else
        {
            var jsonString = await File.ReadAllTextAsync(appSettingsFile, cancellationToken);
            content = JsonNode.Parse(jsonString);
        }

        if (content is null)
        {
            _logger.LogError($"Failed to parse appsettings.json file at {appSettingsFile}");
            return;            
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
            await File.WriteAllTextAsync(appSettingsFile, content.ToJsonString(options));
            _logger.LogInformation($"Updated '{Path.GetFileName(appSettingsFile)}' with connection string '{ConnectionStringName}'");
        }
    }
}
