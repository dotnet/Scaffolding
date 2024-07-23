// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using Microsoft.DotNet.Scaffolding.Helpers.General;
using Microsoft.DotNet.Scaffolding.Helpers.Services;
using Microsoft.DotNet.Scaffolding.Helpers.Templates.DbContext;
using Microsoft.DotNet.Scaffolding.TextTemplating;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;

namespace Microsoft.DotNet.Scaffolding.Helpers.Steps;

internal class AddNewDbContextStep : ScaffoldStep
{
    public required ILogger Logger { get; init; }
    public required IFileSystem FileSystem { get; init; }
    public required string ProjectBaseDirectory { get; init; }
    public required DbContextProperties DbContextProperties { get; init; }

    public async override Task<bool> ExecuteAsync()
    {
        var addedDbContext = false;
        Logger.LogInformation($"Adding new DbContext '{DbContextProperties.DbContextName}'...");
        var addTextTemplateStep = GetAddTextTemplatingStep();
        if (addTextTemplateStep is null)
        {
            return false;
        }

        addedDbContext = await addTextTemplateStep.ExecuteAsync();
        if (addedDbContext)
        {
            AddConnectionString(DbContextProperties);
        }

        return addedDbContext;
    }

    private AddTextTemplatingStep? GetAddTextTemplatingStep()
    {
        //get .tt template file path
        var templateUtilities = new TemplateFoldersUtilities();
        var allT4Templates = templateUtilities.GetAllT4Templates(["DbContext"]);
        string? t4TemplatePath = allT4Templates.FirstOrDefault(x => x.EndsWith("NewDbContext.tt", StringComparison.OrdinalIgnoreCase));
        //get System.Type for NewDbContext
        var templateType = typeof(NewDbContext);
        if (string.IsNullOrEmpty(t4TemplatePath) ||
            string.IsNullOrEmpty(DbContextProperties.DbContextPath) ||
            templateType is null)
        {
            return null;
        }

        var addTextTemplateStep = new AddTextTemplatingStep
        {
            TemplatePath = t4TemplatePath,
            TemplateType = templateType,
            TemplateModel = DbContextProperties,
            TemplateModelName = "Model",
            FileSystem = FileSystem,
            Logger = Logger,
            OutputPath = DbContextProperties.DbContextPath
        };

        return addTextTemplateStep;
    }

    private void AddConnectionString(DbContextProperties dbContextProperties)
    {
        string databaseName = dbContextProperties.DbContextName;
        var appSettingsFileSearch = FileSystem.EnumerateFiles(ProjectBaseDirectory, "appsettings.json", SearchOption.AllDirectories);
        var appSettingsFile = appSettingsFileSearch.FirstOrDefault();
        JObject content;
        bool writeContent = false;

        if (string.IsNullOrEmpty(appSettingsFile) || !FileSystem.FileExists(appSettingsFile))
        {
            content = [];
            writeContent = true;
        }
        else
        {
            content = JObject.Parse(FileSystem.ReadAllText(appSettingsFile));
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
            connectionStringObject[databaseName] is null &&
            !string.IsNullOrEmpty(dbContextProperties.NewDbConnectionString))
        {
            string connectionString = string.Format(dbContextProperties.NewDbConnectionString, databaseName);
            writeContent = true;
            connectionStringObject[databaseName] = connectionString;
            content[connectionStringNodeName] = connectionStringObject;
        }

        // Json.Net loses comments so the above code if requires any changes loses
        // comments in the file. The writeContent bool is for saving
        // a specific case without losing comments - when no changes are needed.
        if (writeContent && !string.IsNullOrEmpty(appSettingsFile))
        {
            FileSystem.WriteAllText(appSettingsFile, content.ToString());
        }
    }
}
