// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text;
using Microsoft.DotNet.Scaffolding.Core.Scaffolders;
using Microsoft.DotNet.Scaffolding.Internal.Services;
using Microsoft.DotNet.Scaffolding.Internal.Telemetry;
using Microsoft.DotNet.Tools.Scaffold.AspNet.Telemetry;
using Microsoft.Extensions.Logging;
using TelemetryConstants = Microsoft.DotNet.Tools.Scaffold.AspNet.Telemetry.TelemetryConstants;
namespace Microsoft.DotNet.Scaffolding.Core.Steps;

/// <summary>
/// Scaffold step for adding a connection string to the appsettings.json file of a project.
/// Updates or creates the ConnectionStrings section as needed.
/// </summary>
internal class AddAspNetConnectionStringStep : ScaffoldStep
{
    /// <summary>
    /// Gets or sets the base project path to search for appsettings.json.
    /// </summary>
    public required string BaseProjectPath { get; set; }
    /// <summary>
    /// Gets or sets the name of the connection string.
    /// </summary>
    public required string ConnectionStringName { get; set; }
    /// <summary>
    /// Gets or sets the connection string value.
    /// </summary>
    public required string ConnectionString { get; set; }
    private readonly ILogger _logger;
    private readonly IFileSystem _fileSystem;
    private readonly ITelemetryService _telemetryService;
    /// <summary>
    /// Initializes a new instance of the <see cref="AddAspNetConnectionStringStep"/> class.
    /// </summary>
    public AddAspNetConnectionStringStep(
        ILogger<AddAspNetConnectionStringStep> logger,
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
        var appSettingsFileSearch = _fileSystem.EnumerateFiles(BaseProjectPath, "appsettings.json", SearchOption.AllDirectories);
        var appSettingsFile = appSettingsFileSearch.FirstOrDefault();
        JsonNode? content;
        bool writeContent = false;
        string? originalContent = null;
        if (string.IsNullOrEmpty(appSettingsFile) || !_fileSystem.FileExists(appSettingsFile))
        {
            content = new JsonObject();
            writeContent = true;
        }
        else
        {
            try
            {
                originalContent = _fileSystem.ReadAllText(appSettingsFile);
                content = JsonNode.Parse(originalContent, new JsonNodeOptions { PropertyNameCaseInsensitive = true }, new JsonDocumentOptions
                {
                    CommentHandling = JsonCommentHandling.Skip,
                    AllowTrailingCommas = true
                });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Failed to parse appsettings.json file at {appSettingsFile}: {ex.Message}");
                _telemetryService.TrackEvent(new AddAspNetConnectionStringTelemetryEvent(context.Scaffolder.DisplayName, TelemetryConstants.Failure, "Failed to parse appsettings.json"));
                return Task.FromResult(false);
            }
        }

        if (content is not JsonObject)
        {
            _logger.LogError("Failed to parse appsettings.json file at {File}: root JSON value must be an object.", appSettingsFile);
            _telemetryService.TrackEvent(new AddAspNetConnectionStringTelemetryEvent(context.Scaffolder.DisplayName, TelemetryConstants.Failure, "Failed to parse appsettings.json"));
            return Task.FromResult(false);
        }

        string connectionStringNodeName = "ConnectionStrings";
        bool createSection = content[connectionStringNodeName] is null;

        if (!createSection && content[connectionStringNodeName] is not JsonObject)
        {
            _logger.LogError("Failed to update '{File}': ConnectionStrings must be an object or null.", appSettingsFile);
            _telemetryService.TrackEvent(new AddAspNetConnectionStringTelemetryEvent(context.Scaffolder.DisplayName,
                TelemetryConstants.Failure, "Invalid ConnectionStrings section"));
            return Task.FromResult(false);
        }

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
            string updatedContent = originalContent is null ? content.ToJsonString(options)
                : createSection
                    ? SetJsonProperty(originalContent, null, connectionStringNodeName, content[connectionStringNodeName]!.ToJsonString())
                    : SetJsonProperty(originalContent, connectionStringNodeName, ConnectionStringName, JsonSerializer.Serialize(ConnectionString));
            _fileSystem.WriteAllText(appSettingsFile, updatedContent);
            _logger.LogInformation($"Updated '{Path.GetFileName(appSettingsFile)}' with connection string '{ConnectionStringName}'");
            _telemetryService.TrackEvent(new AddAspNetConnectionStringTelemetryEvent(context.Scaffolder.Name, TelemetryConstants.Added));
        }
        else
        {
            _telemetryService.TrackEvent(new AddAspNetConnectionStringTelemetryEvent(context.Scaffolder.Name, TelemetryConstants.NoChange));
        }

        return Task.FromResult(true);
    }

    private static string SetJsonProperty(string json, string? sectionName, string propertyName, string serializedValue)
    {
        byte[] utf8 = Encoding.UTF8.GetBytes(json);
        var reader = new Utf8JsonReader(utf8,
            new JsonReaderOptions { CommentHandling = JsonCommentHandling.Skip, AllowTrailingCommas = true });
        reader.Read();
        if (sectionName is not null)
        {
            while (reader.Read())
            {
                if (reader.TokenType == JsonTokenType.PropertyName && reader.CurrentDepth == 1 &&
                    string.Equals(reader.GetString(), sectionName, StringComparison.OrdinalIgnoreCase))
                {
                    reader.Read();
                    break;
                }
            }
        }

        if (reader.TokenType != JsonTokenType.StartObject)
        {
            throw new JsonException("The configuration section must be a JSON object.");
        }

        int insertionIndex = Encoding.UTF8.GetCharCount(utf8.AsSpan(0, (int)reader.BytesConsumed));
        int objectDepth = reader.CurrentDepth;
        int? firstPropertyIndex = null;
        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.EndObject && reader.CurrentDepth == objectDepth)
            {
                int closingIndex = Encoding.UTF8.GetCharCount(utf8.AsSpan(0, (int)reader.TokenStartIndex));
                string property = $"{JsonSerializer.Serialize(propertyName)}: {serializedValue}";
                if (firstPropertyIndex is not null)
                {
                    property += ",";
                }

                if (json.AsSpan(insertionIndex, closingIndex - insertionIndex).Contains('\n'))
                {
                    int referenceIndex = firstPropertyIndex ?? closingIndex;
                    int lineStart = json.LastIndexOf('\n', referenceIndex - 1) + 1;
                    string indent = new string(json[lineStart..referenceIndex].TakeWhile(c => c is ' ' or '\t').ToArray());
                    if (firstPropertyIndex is null)
                    {
                        indent += "  ";
                    }

                    string newline = json.Contains("\r\n", StringComparison.Ordinal) ? "\r\n" : "\n";
                    property = newline + indent + property;
                }

                // Prepending avoids changing existing trailing commas and comments.
                return json.Insert(insertionIndex, property);
            }

            if (reader.TokenType == JsonTokenType.PropertyName && reader.CurrentDepth == objectDepth + 1)
            {
                firstPropertyIndex ??= Encoding.UTF8.GetCharCount(utf8.AsSpan(0, (int)reader.TokenStartIndex));
                bool matches = string.Equals(reader.GetString(), propertyName, StringComparison.OrdinalIgnoreCase);
                reader.Read();
                int valueStartByte = (int)reader.TokenStartIndex;
                reader.Skip();
                if (matches)
                {
                    int valueStart = Encoding.UTF8.GetCharCount(utf8.AsSpan(0, valueStartByte));
                    int valueEnd = Encoding.UTF8.GetCharCount(utf8.AsSpan(0, (int)reader.BytesConsumed));
                    return json[..valueStart] + serializedValue + json[valueEnd..];
                }
            }
        }

        throw new JsonException("The configuration section is incomplete.");
    }
}
