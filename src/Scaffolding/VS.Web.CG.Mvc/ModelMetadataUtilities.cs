// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.DotNet.Scaffolding.Shared.Project;
using Microsoft.VisualStudio.Web.CodeGeneration;
using Microsoft.VisualStudio.Web.CodeGeneration.EntityFrameworkCore;
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
            CommonCommandLineModel commandLineModel, 
            IEntityFrameworkService entityFrameworkService, 
            IModelTypesLocator modelTypesLocator,
            string areaName)
        {
            ModelType model = ValidationUtil.ValidateType(commandLineModel.ModelClass, "model", modelTypesLocator);
            ModelType dataContext = ValidationUtil.ValidateType(commandLineModel.DataContextClass, "dataContext", modelTypesLocator, throwWhenNotFound: false);

            // Validation successful
            Contract.Assert(model != null, MessageStrings.ValidationSuccessfull_modelUnset);

            var dbContextFullName = dataContext != null ? dataContext.FullName : commandLineModel.DataContextClass;

            var modelMetadata = await entityFrameworkService.GetModelMetadata(
                dbContextFullName,
                model,
                areaName,
                commandLineModel.UseSqlite);

            return new ModelTypeAndContextModel()
            {
                ModelType = model,
                DbContextFullName = dbContextFullName,
                ContextProcessingResult = modelMetadata,
                UseSqlite = commandLineModel.UseSqlite
            };
        }

        internal static async Task<ModelTypeAndContextModel> GetModelEFMetadataMinimalAsync(
            MinimalApiGeneratorCommandLineModel commandLineModel,
            IEntityFrameworkService entityFrameworkService,
            IModelTypesLocator modelTypesLocator,
            string areaName)
        {
            ModelType model = ValidationUtil.ValidateType(commandLineModel.ModelClass, "model", modelTypesLocator);
            // Validation successful
            Contract.Assert(model != null, MessageStrings.ValidationSuccessfull_modelUnset);

            ModelType dataContext = null;
            var dbContextFullName = string.Empty;
            ContextProcessingResult modelMetadata  = new ContextProcessingResult()
            {
                ContextProcessingStatus = ContextProcessingStatus.MissingContext,
                ModelMetadata = null
            };

            if (!string.IsNullOrEmpty(commandLineModel.DataContextClass))
            {
                dataContext = ValidationUtil.ValidateType(commandLineModel.DataContextClass, "dataContext", modelTypesLocator, throwWhenNotFound: false);
                dbContextFullName = dataContext != null ? dataContext.FullName : commandLineModel.DataContextClass;

                modelMetadata = await entityFrameworkService.GetModelMetadata(
                    dbContextFullName,
                    model,
                    areaName,
                    useSqlite: false);
            }

            return new ModelTypeAndContextModel()
            {
                ModelType = model,
                DbContextFullName = dbContextFullName,
                ContextProcessingResult = modelMetadata,
                UseSqlite = false
            };
        }
    }
}
