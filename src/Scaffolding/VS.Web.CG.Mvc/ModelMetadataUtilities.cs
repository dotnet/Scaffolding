// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.DotNet.Scaffolding.Shared;
using Microsoft.DotNet.Scaffolding.Shared.Project;
using Microsoft.VisualStudio.Web.CodeGeneration.EntityFrameworkCore;
using Microsoft.VisualStudio.Web.CodeGenerators.Mvc.Blazor;
using Microsoft.VisualStudio.Web.CodeGenerators.Mvc.MinimalApi;

namespace Microsoft.VisualStudio.Web.CodeGenerators.Mvc
{
    internal static class ModelMetadataUtilities
    {
        internal static async Task<ModelTypeAndContextModel> ValidateModelAndGetCodeModelMetadata(
            CommonCommandLineModel commandLineModel,
            ICodeModelService codeModeService,
            IModelTypesLocator modelTypesLocator)
        {
            ModelType model = ValidationUtil.ValidateType(commandLineModel.ModelClass, "model", modelTypesLocator);

            Contract.Assert(model != null, MessageStrings.ValidationSuccessfull_modelUnset);
            var result = await codeModeService.GetModelMetadata(model);

            return new ModelTypeAndContextModel()
            {
                ModelType = model,
                ContextProcessingResult = result
            };
        }

        internal static async Task<ModelTypeAndContextModel> ValidateModelAndGetEFMetadata(
           string modelClass,
           string dbContextClass,
           string dbProviderString,
           IEntityFrameworkService entityFrameworkService,
           IModelTypesLocator modelTypesLocator,
           ILogger logger,
           string areaName,
           bool useDbFactory = false)
        {
            ModelType model = ValidationUtil.ValidateType(modelClass, "model", modelTypesLocator);
            // Validation successful
            Contract.Assert(model != null, MessageStrings.ValidationSuccessfull_modelUnset);

            ModelType dataContext = null;
            DbProvider dbProvider = DbProvider.None;
            var dbContextFullName = string.Empty;
            ContextProcessingResult modelMetadata = new ContextProcessingResult()
            {
                ContextProcessingStatus = ContextProcessingStatus.MissingContext,
                ModelMetadata = null
            };

            if (!string.IsNullOrEmpty(dbContextClass))
            {
                dataContext = ValidationUtil.ValidateType(dbContextClass, "dataContext", modelTypesLocator, throwWhenNotFound: false);
                dbContextFullName = dataContext != null ? dataContext.FullName : dbContextClass;
                dbProvider = dataContext == null ? ValidateDatabaseProvider(dbProviderString, logger) : DbProvider.Existing;

                modelMetadata = await entityFrameworkService.GetModelMetadata(
                    dbContextFullName,
                    model,
                    areaName,
                    databaseProvider: dbProvider,
                    useDbFactory);
            }

            return new ModelTypeAndContextModel()
            {
                ModelType = model,
                DbContextFullName = dbContextFullName,
                ContextProcessingResult = modelMetadata,
                DatabaseProvider = dbProvider
            };
        }

        internal static async Task<ModelTypeAndContextModel> ValidateModelAndGetEFMetadata(
            CommonCommandLineModel commandLineModel,
            IEntityFrameworkService entityFrameworkService,
            IModelTypesLocator modelTypesLocator,
            ILogger logger,
            string areaName)
        {
            return await ValidateModelAndGetEFMetadata(
                commandLineModel.ModelClass,
                commandLineModel.DataContextClass,
                commandLineModel.DatabaseProviderString,
                entityFrameworkService,
                modelTypesLocator,
                logger,
                areaName);
        }

        internal static async Task<ModelTypeAndContextModel> GetModelEFMetadataBlazorAsync(
            BlazorWebCRUDGeneratorCommandLineModel commandLineModel,
            IEntityFrameworkService entityFrameworkService,
            IModelTypesLocator modelTypesLocator,
            ILogger logger,
            string areaName)
        {
            return await ValidateModelAndGetEFMetadata(
                commandLineModel.ModelClass,
                commandLineModel.DataContextClass,
                commandLineModel.DatabaseProviderString,
                entityFrameworkService,
                modelTypesLocator,
                logger,
                areaName,
                useDbFactory: true);
        }

        internal static async Task<ModelTypeAndContextModel> GetModelEFMetadataMinimalAsync(
            MinimalApiGeneratorCommandLineModel commandLineModel,
            IEntityFrameworkService entityFrameworkService,
            IModelTypesLocator modelTypesLocator,
            ILogger logger,
            string areaName)
        {
            return await ValidateModelAndGetEFMetadata(
                commandLineModel.ModelClass,
                commandLineModel.DataContextClass,
                commandLineModel.DatabaseProviderString,
                entityFrameworkService,
                modelTypesLocator,
                logger,
                areaName);
        }

        internal static DbProvider ValidateDatabaseProvider(string databaseProviderString, ILogger logger, bool isIdentity = false)
        {
            var dbProviders = isIdentity ? EfConstants.IdentityDbProviders : EfConstants.AllDbProviders;
            if (string.IsNullOrEmpty(databaseProviderString))
            {
                logger.LogMessage(MessageStrings.NoDbProviderFound, LogMessageLevel.Information);
                logger.LogMessage("Using 'SQL Server' by default!", LogMessageLevel.Information);
                return DbProvider.SqlServer;
            }
            else if (dbProviders.TryGetValue(databaseProviderString, out DbProvider dbProvider))
            {
                return dbProvider;
            }
            else
            {
                logger.LogMessage($"Invalid database provider '{databaseProviderString}' provided.", LogMessageLevel.Information);
                logger.LogMessage("Using 'SQL Server' by default!", LogMessageLevel.Information);
                return DbProvider.SqlServer;
            }
        }
    }
}
