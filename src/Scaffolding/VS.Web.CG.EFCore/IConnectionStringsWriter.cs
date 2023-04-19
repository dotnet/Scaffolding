// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

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
