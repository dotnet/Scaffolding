// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using Microsoft.DotNet.Scaffolding.ComponentModel;
using Microsoft.DotNet.Scaffolding.Helpers.General;
using Spectre.Console;
using Spectre.Console.Cli;

namespace Microsoft.DotNet.Tools.Scaffold.Aspire.Commands
{
    public class GetCmdsCommand : Command
    {
        public override int Execute(CommandContext context)
        {
            var commands = new List<CommandInfo>
            {
                new()
                {
                    Name = "caching",
                    DisplayName = "Caching",
                    DisplayCategory = "Aspire",
                    Description = "Modifies Aspire project to make it caching ready!",
                    Parameters = GetCmdsHelper.CachingParameters
                },
                new()
                {
                    Name = "storage",
                    DisplayName = "Storage",
                    DisplayCategory = "Aspire",
                    Description = "Modifies Aspire project to make it storage ready!",
                    Parameters = GetCmdsHelper.StorageParameters
                },
                new()
                {
                    Name = "database",
                    DisplayName = "Database",
                    DisplayCategory = "Aspire",
                    Description = "Modifies Aspire project to make it database ready!",
                    Parameters = GetCmdsHelper.DatabaseParameters
                }
                // Add other commands here
            };

            var json = System.Text.Json.JsonSerializer.Serialize(commands);
            AnsiConsole.WriteLine(json);
            return 0;
        }
    }

    public static class GetCmdsHelper
    {
        internal static Parameter AppHostProjectParameter = new() { Name = "--apphost-project", DisplayName = "Aspire App host project file", Description = "Aspire App host project for the scaffolding", Required = true, Type = BaseTypes.String, PickerType = InteractivePickerType.ProjectPicker };
        internal static Parameter ProjectParameter = new() { Name = "--project", DisplayName = "Web or worker project file", Description = "Web or worker project associated with the Aspire App host", Required = true, Type = BaseTypes.String, PickerType = InteractivePickerType.ProjectPicker };
        internal static DbContextProperties SqlServerDefaults = new()
        {
            DbType = "sqlserver",
            DbName = "sqldb",
            AddDbMethod = "AddSqlServer",
            AddDbContextMethod = "AddSqlServerDbContext"
        };

        internal static DbContextProperties NpgsqlDefaults = new()
        {
            DbType = "postgresql",
            DbName = "postgresqldb",
            AddDbMethod = "AddPostgres",
            AddDbContextMethod = "AddNpgsqlDbContext"
        };

        internal static List<string> CachingTypeCustomValues = ["redis", "redis-with-output-caching"];
        internal static Dictionary<string, DbContextProperties?> DatabaseTypeDefaults = new()
        {
            { "npgsql-efcore", NpgsqlDefaults },
            { "sqlserver-efcore", SqlServerDefaults }
        };

        internal static List<string> DatabaseTypeCustomValues = [.. DatabaseTypeDefaults.Keys];
        internal static List<string> StorageTypeCustomValues = ["azure-storage-queues", "azure-storage-blobs", "azure-data-tables"];

        internal static Parameter CachingTypeParameter = new() { Name = "--type", DisplayName = "Caching type", Description = "Types of caching", Required = true, Type = BaseTypes.String, PickerType = InteractivePickerType.CustomPicker, CustomPickerValues = CachingTypeCustomValues };
        internal static Parameter DatabaseTypeParameter = new() { Name = "--type", DisplayName = "Database type", Description = "Types of database", Required = true, Type = BaseTypes.String, PickerType = InteractivePickerType.CustomPicker, CustomPickerValues = DatabaseTypeCustomValues };
        internal static Parameter StorageTypeParameter = new() { Name = "--type", DisplayName = "Storage type", Description = "Types of storage", Required = true, Type = BaseTypes.String, PickerType = InteractivePickerType.CustomPicker, CustomPickerValues = StorageTypeCustomValues };

        internal static Parameter PrereleaseParameter = new() { Name = "--prerelease", DisplayName = "Include prerelease packages?", Description = "Include prerelease package versions when installing latest Aspire components", Required = true, Type = BaseTypes.Bool, PickerType = InteractivePickerType.YesNo };

        internal static Parameter[] CachingParameters = [CachingTypeParameter, AppHostProjectParameter, ProjectParameter, PrereleaseParameter];
        internal static Parameter[] DatabaseParameters = [DatabaseTypeParameter, AppHostProjectParameter, ProjectParameter, PrereleaseParameter];
        internal static Parameter[] StorageParameters = [StorageTypeParameter, AppHostProjectParameter, ProjectParameter, PrereleaseParameter];
    }
}
