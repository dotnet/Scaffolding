// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.DotNet.Scaffolding.Shared;
using Microsoft.VisualStudio.Web.CodeGeneration.CommandLine;

namespace Microsoft.VisualStudio.Web.CodeGenerators.Mvc.MinimalApi
{
    public class MinimalApiGeneratorCommandLineModel
    {
        [Option(Name = "endpoints", ShortName = "e", Description = "Endpoints class to use. (not file name)")]
        public string EndpintsClassName { get; set; }

        [Option(Name = "model", ShortName = "m", Description = "Model class to use")]
        public string ModelClass { get; set; }

        [Option(Name = "dataContext", ShortName = "dc", Description = "DbContext class to use")]
        public string DataContextClass { get; set; }

        [Option(Name = "relativeFolderPath", ShortName = "outDir", Description = "Specify the relative output folder path from project where the file needs to be generated, if not specified, file will be generated in the project folder")]
        public string RelativeFolderPath { get; set; }

        [Option(Name = "open", ShortName = "o", Description = "Use this option to enable OpenAPI")]
        public bool OpenApi { get; set; }

        [Option(Name = "endpointsNamespace", ShortName = "namespace", Description = "Specify the name of the namespace to use for the generated controller")]
        public string EndpointsNamespace { get; set; }

        [Obsolete("Use --databaseProvider or -dbProvider to configure database type instead")]
        [Option(Name = "useSqLite", ShortName = "sqlite", Description = "Flag to specify if DbContext should use SQLite instead of SQL Server.")]
        public bool UseSqlite { get; set; }

        //adding UseSqlite2 for backwards compat. for VS Mac scenarios (casing issue).
        [Obsolete("Use --databaseProvider or -dbProvider to configure database type instead")]
        [Option(Name = "useSqlite", Description = "Flag to specify if DbContext should use SQLite instead of SQL Server.")]
        public bool UseSqlite2 { get; set; }

        [Option(Name = "databaseProvider", ShortName = "dbProvider", Description = "Database provider to use. Options include 'sqlserver' (default), 'sqlite', 'cosmos', 'postgres'.")]
        public string DatabaseProviderString { get; set; }
        public DbProvider DatabaseProvider { get; set; }

        [Option(Name = "noTypedResults", ShortName = "ntr", Description = "Flag to not use TypedResults for minimal apis.")]
        public bool NoTypedResults { get; set; }

        public MinimalApiGeneratorCommandLineModel()
        {
        }

        protected MinimalApiGeneratorCommandLineModel(MinimalApiGeneratorCommandLineModel copyFrom)
        {
            EndpintsClassName = copyFrom.EndpintsClassName;
            ModelClass = copyFrom.ModelClass;
            RelativeFolderPath = copyFrom.RelativeFolderPath;
            OpenApi = copyFrom.OpenApi;
            EndpointsNamespace = copyFrom.EndpointsNamespace;
            DatabaseProvider = copyFrom.DatabaseProvider;
            NoTypedResults = copyFrom.NoTypedResults;
        }

        public MinimalApiGeneratorCommandLineModel Clone()
        {
            return new MinimalApiGeneratorCommandLineModel(this);
        }
    }

    public static class MinimalApiGeneratorCommandLineModelExtensions
    {
        public static void ValidateCommandline(this MinimalApiGeneratorCommandLineModel model, ILogger logger)
        {
            if (model == null)
            {
                throw new ArgumentNullException(nameof(model));
            }

#pragma warning disable CS0618 // Type or member is obsolete
            if (model.UseSqlite || model.UseSqlite2)
            {
#pragma warning restore CS0618 // Type or member is obsolete
                //instead of throwing an error, letting the devs know that its obsolete.
                logger.LogMessage(MessageStrings.SqliteObsoleteOption, LogMessageLevel.Information);
                //Setting DatabaseProvider to SQLite if --databaseProvider|-dbProvider is not provided.
                if (string.IsNullOrEmpty(model.DatabaseProviderString))
                {
                    model.DatabaseProvider = DbProvider.SQLite;
                    model.DatabaseProviderString = EfConstants.SQLite;
                }
            }
            else
            {
                if (!string.IsNullOrEmpty(model.DatabaseProviderString) && EfConstants.AllDbProviders.TryGetValue(model.DatabaseProviderString, out var dbProvider))
                {
                    model.DatabaseProvider = dbProvider;
                }
            }
        }
    }
}
