// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using Microsoft.DotNet.Scaffolding.Helpers.Services;
using Microsoft.DotNet.Scaffolding.Helpers.T4Templating;
using Microsoft.DotNet.Scaffolding.Helpers.Templates.DbContext;

namespace Microsoft.DotNet.Scaffolding.Helpers.General;

public static class DbContextHelper
{
    public static DbContextProperties SqlServerDefaults = new()
    {
        DbType = "sqlserver",
        DbName = "sqldb",
        AddDbMethod = "AddSqlServer",
        AddDbContextMethod = "AddSqlServerDbContext"
    };

    public static DbContextProperties NpgsqlDefaults = new()
    {
        DbType = "postgresql",
        DbName = "postgresqldb",
        AddDbMethod = "AddPostgres",
        AddDbContextMethod = "AddNpgsqlDbContext"
    };

    public static Dictionary<string, DbContextProperties?> DatabaseTypeDefaults = new()
    {
        { "npgsql-efcore", NpgsqlDefaults },
        { "sqlserver-efcore", SqlServerDefaults }
    };

    public static List<string> DatabaseTypes = DatabaseTypeDefaults.Keys.ToList();

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

    public static bool CreateDbContext(DbContextProperties dbContextProperties, string dbContextPath, IFileSystem fileSystem)
    {
        var templateUtilities = new TemplateFoldersUtilities();
        var allT4Templates = templateUtilities.GetAllT4Templates(["DbContext"]);
        string? t4TemplatePath = allT4Templates.FirstOrDefault(x => x.EndsWith("NewDbContext.tt", StringComparison.OrdinalIgnoreCase));
        ITextTransformation? textTransformation = GetDbContextTransformation(t4TemplatePath);
        if (textTransformation is null)
        {
            throw new Exception($"Unable to process T4 template for a new DbContext correctly");
        }

        var templateInvoker = new TemplateInvoker();
        var dictParams = new Dictionary<string, object>()
            {
                { "Model" , dbContextProperties }
            };

        var t4TemplateName = Path.GetFileNameWithoutExtension(t4TemplatePath);
        var templatedString = templateInvoker.InvokeTemplate(textTransformation, dictParams);
        if (!string.IsNullOrEmpty(templatedString) && !string.IsNullOrEmpty(dbContextPath))
        {
            fileSystem.WriteAllText(dbContextPath, templatedString);
            return true;
        }

        return false;
    }
}
