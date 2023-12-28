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

        [Option(Name = "databaseProvider", ShortName = "dbProvider", Description = "Database provider to use. Options (without quotes) include 'sqlserver' (default), 'sqlite', 'cosmos', 'postgres'. Example: -dbProvider sqlite")]
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

            if (!string.IsNullOrEmpty(model.DatabaseProviderString) && EfConstants.AllDbProviders.TryGetValue(model.DatabaseProviderString, out var dbProvider))
            {
                model.DatabaseProvider = dbProvider;
            }
        }
    }
}
