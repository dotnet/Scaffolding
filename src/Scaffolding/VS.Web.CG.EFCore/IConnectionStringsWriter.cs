// Copyright (c) .NET Foundation. All rights reserved.
using System;
using Microsoft.DotNet.Scaffolding.Shared;

namespace Microsoft.VisualStudio.Web.CodeGeneration.EntityFrameworkCore
{
    public interface IConnectionStringsWriter
    {
        [Obsolete]
        void AddConnectionString(string connectionStringName, string dataBaseName, bool useSqlite);
        void AddConnectionString(string connectionStringName, string databaseName, DbProvider databaseProvider);
    }
}
