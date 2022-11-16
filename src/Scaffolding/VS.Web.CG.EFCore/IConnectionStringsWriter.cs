// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace Microsoft.VisualStudio.Web.CodeGeneration.EntityFrameworkCore
{
    public interface IConnectionStringsWriter
    {
        [Obsolete]
        void AddConnectionString(string connectionStringName, string dataBaseName, bool useSqlite);
        void AddConnectionString(string connectionStringName, string databaseName, string databaseType);
    }
}
