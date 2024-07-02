// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using Microsoft.DotNet.Scaffolding.Helpers.Services;
using Microsoft.DotNet.Scaffolding.Helpers.T4Templating;
using Microsoft.DotNet.Scaffolding.Helpers.Templates.DbContext;
using Newtonsoft.Json.Linq;

namespace Microsoft.DotNet.Scaffolding.Helpers.General;

internal static class DbContextHelper
{
    public static ITextTransformation? GetDbContextTransformation(string? t4TemplatePath)
    {
        if (string.IsNullOrEmpty(t4TemplatePath))
        {
            return null;
        }

        var host = new TextTemplatingEngineHost { TemplateFile = t4TemplatePath };
        ITextTransformation? transformation = new NewDbContext() { Host = host }; ;

        if (transformation != null)
        {
            transformation.Session = host.CreateSession();
        }

        return transformation;
    }

    public static bool CreateDbContext(DbContextProperties dbContextProperties, string dbContextPath, string? projectBasePath, IFileSystem fileSystem)
    {
        var templateUtilities = new TemplateFoldersUtilities();
        var allT4Templates = templateUtilities.GetAllT4Templates(["DbContext"]);
        string? t4TemplatePath = allT4Templates.FirstOrDefault(x => x.EndsWith("NewDbContext.tt", StringComparison.OrdinalIgnoreCase));
        ITextTransformation? textTransformation = GetDbContextTransformation(t4TemplatePath) ?? throw new Exception($"Unable to process T4 template for a new DbContext correctly");
        var templateInvoker = new TemplateInvoker();
        var dictParams = new Dictionary<string, object>()
        {
            { "Model" , dbContextProperties }
        };

        var t4TemplateName = Path.GetFileNameWithoutExtension(t4TemplatePath);
        var templatedString = templateInvoker.InvokeTemplate(textTransformation, dictParams);
        if (!string.IsNullOrEmpty(templatedString) &&
            !string.IsNullOrEmpty(dbContextPath) &&
            !string.IsNullOrEmpty(projectBasePath))
        {
            fileSystem.WriteAllText(dbContextPath, templatedString);
            AddConnectionString(dbContextProperties, projectBasePath, fileSystem);
            return true;
        }

        return false;
    }

    public static void AddConnectionString(DbContextProperties dbContextProperties, string projectBasePath, IFileSystem fileSystem)
    {
        string databaseName = dbContextProperties.DbContextName;
        var appSettingsFileSearch = fileSystem.EnumerateFiles(projectBasePath, "appsettings.Json", SearchOption.AllDirectories);
        var appSettingsFile = appSettingsFileSearch.FirstOrDefault();
        JObject content;
        bool writeContent = false;

        if (string.IsNullOrEmpty(appSettingsFile) || !fileSystem.FileExists(appSettingsFile))
        {
            content = [];
            writeContent = true;
        }
        else
        {
            content = JObject.Parse(fileSystem.ReadAllText(appSettingsFile));
        }

        string connectionStringNodeName = "ConnectionStrings";

        if (content[connectionStringNodeName] is null)
        {
            writeContent = true;
            content[connectionStringNodeName] = new JObject();
        }

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
            fileSystem.WriteAllText(appSettingsFile, content.ToString());
        }
    }
}
