// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using Microsoft.DotNet.Scaffolding.Core.Scaffolders;
using Newtonsoft.Json.Linq;
using ScaffoldingStep = Microsoft.DotNet.Scaffolding.Core.Steps.ScaffoldStep;

namespace Microsoft.DotNet.Scaffolding.Helpers.Steps;

internal class AddConnectionStringStep : ScaffoldingStep
{
    public required string BaseProjectPath { get; set; }
    public required string ConnectionStringName { get; set; }
    public required string ConnectionString { get; set; }

    public override Task ExecuteAsync(ScaffolderContext context, CancellationToken cancellationToken = default)
    {
        var appSettingsFileSearch = Directory.EnumerateFiles(BaseProjectPath, "appsettings.json", SearchOption.AllDirectories);
        var appSettingsFile = appSettingsFileSearch.FirstOrDefault();
        JObject content;
        bool writeContent = false;

        if (string.IsNullOrEmpty(appSettingsFile) || !File.Exists(appSettingsFile))
        {
            content = [];
            writeContent = true;
        }
        else
        {
            content = JObject.Parse(File.ReadAllText(appSettingsFile));
        }

        string connectionStringNodeName = "ConnectionStrings";

        //find the "ConnectionStrings" node.
        if (content[connectionStringNodeName] is null)
        {
            writeContent = true;
            content[connectionStringNodeName] = new JObject();
        }

        //if a key with the 'databaseName' already exists, skipping adding a connection string.
        if (content[connectionStringNodeName] is JObject connectionStringObject &&
            connectionStringObject[ConnectionStringName] is null &&
            !string.IsNullOrEmpty(ConnectionString))
        {
            writeContent = true;
            connectionStringObject[ConnectionStringName] = ConnectionString;
            content[connectionStringNodeName] = connectionStringObject;
        }

        // Json.Net loses comments so the above code if requires any changes loses
        // comments in the file. The writeContent bool is for saving
        // a specific case without losing comments - when no changes are needed.
        if (writeContent && !string.IsNullOrEmpty(appSettingsFile))
        {
            File.WriteAllText(appSettingsFile, content.ToString());
            return Task.FromResult(true);
        }

        return Task.FromResult(false);
    }
}
