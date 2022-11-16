// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using Microsoft.DotNet.Scaffolding.Shared;
using Microsoft.VisualStudio.Web.CodeGeneration.DotNet;
using Newtonsoft.Json.Linq;

namespace Microsoft.VisualStudio.Web.CodeGeneration.EntityFrameworkCore
{
    public class ConnectionStringsWriter : IConnectionStringsWriter
    {
        private const string SQLConnectionStringFormat = "Server=(localdb)\\mssqllocaldb;Database={0};Trusted_Connection=True;MultipleActiveResultSets=true";
        private const string SQLiteConnectionStringFormat = "Data Source={0}.db";
        private const string CosmosDbConnectionStringFormat = "AccountEndpoint={0};AccountKey={1}";
        private IDictionary<string, string> ConnectionStringsDict = new Dictionary<string, string>
        {
            { EfConstants.SqlServer, SQLConnectionStringFormat },
            { EfConstants.SQLite, SQLiteConnectionStringFormat },
            { EfConstants.CosmosDb, CosmosDbConnectionStringFormat }
        };

        private IApplicationInfo _applicationInfo;
        private IFileSystem _fileSystem;

        public ConnectionStringsWriter(IApplicationInfo applicationInfo)
            : this(applicationInfo, DefaultFileSystem.Instance)
        {
        }

        internal ConnectionStringsWriter(
            IApplicationInfo applicationInfo,
            IFileSystem fileSystem)
        {
            _applicationInfo = applicationInfo ?? throw new ArgumentNullException(nameof(applicationInfo));
            _fileSystem = fileSystem ?? throw new ArgumentNullException(nameof(fileSystem));
        }

        public void AddConnectionString(string connectionStringName, string dataBaseName, bool useSqlite)
        {
            AddConnectionString(connectionStringName, dataBaseName, useSqlite ? EfConstants.SQLite : EfConstants.SqlServer);
        }

        public void AddConnectionString(string connectionStringName, string databaseName, string databaseType)
        {
            var appSettingsFile = Path.Combine(_applicationInfo.ApplicationBasePath, "appsettings.json");
            JObject content;
            bool writeContent = false;

            if (!_fileSystem.FileExists(appSettingsFile))
            {
                content = new JObject();
                writeContent = true;
            }
            else
            {
                content = JObject.Parse(_fileSystem.ReadAllText(appSettingsFile));
            }

            string connectionStringNodeName = "ConnectionStrings";

            if (content[connectionStringNodeName] == null)
            {
                writeContent = true;
                content[connectionStringNodeName] = new JObject();
            }

            if (content[connectionStringNodeName][connectionStringName] == null)
            {

                var connectionString = string.Format(
                    useSqlite ? SQLiteConnectionStringFormat : SQLConnectionStringFormat,
                    databaseName);
                writeContent = true;
                content[connectionStringNodeName][connectionStringName] = connectionString;
            }

            // Json.Net loses comments so the above code if requires any changes loses
            // comments in the file. The writeContent bool is for saving
            // a specific case without losing comments - when no changes are needed.
            if (writeContent)
            {
                _fileSystem.WriteAllText(appSettingsFile, content.ToString());
            }
        }
    }
}
