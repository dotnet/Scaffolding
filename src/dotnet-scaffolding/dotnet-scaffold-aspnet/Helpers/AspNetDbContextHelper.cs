// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using System.Collections.Generic;
using Microsoft.DotNet.Scaffolding.Helpers.General;

namespace Microsoft.DotNet.Tools.Scaffold.AspNet.Helpers;

internal class AspNetDbContextHelper
{
    internal static DbContextProperties SqlServerDefaults = new()
    {
        AddDbMethod = "AddSqlServer",
    };

    internal static DbContextProperties SqliteDefaults = new()
    {
        AddDbMethod = "AddSqlite",
    };

    internal static DbContextProperties CosmosDefaults = new()
    {
        AddDbMethod = "AddCosmos",
    };

    internal static DbContextProperties NpgsqlDefaults = new()
    {
        AddDbMethod = "AddPostgres",
    };

    internal static Dictionary<string, DbContextProperties?> DatabaseTypeDefaults = new()
    {
        { "npgsql-efcore", NpgsqlDefaults },
        { "sqlserver-efcore", SqlServerDefaults },
        { "sqlite-efcore", SqliteDefaults },
        { "cosmos-efcore", CosmosDefaults }
    };
}
