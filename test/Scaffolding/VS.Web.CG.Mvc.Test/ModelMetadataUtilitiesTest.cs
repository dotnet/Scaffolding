// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.DotNet.Scaffolding.Shared;
using Microsoft.DotNet.Scaffolding.Shared.Project;
using Microsoft.VisualStudio.Web.CodeGeneration;
using Microsoft.VisualStudio.Web.CodeGeneration.EntityFrameworkCore;
using Microsoft.VisualStudio.Web.CodeGenerators.Mvc.MinimalApi;
using Moq;
using Xunit;

namespace Microsoft.VisualStudio.Web.CodeGenerators.Mvc.Test
{
    public class ModelMetadataUtilitiesTest
    {
        private Mock<IEntityFrameworkService> efService;
        private Mock<ICodeModelService> codeModelService;
        private CommonCommandLineModel model;
        private Mock<IModelTypesLocator> modelTypesLocator;
        private Mock<IModelTypesLocator> modelTypesLocatorWithoutContext;
        private Mock<ILogger> logger;

        public ModelMetadataUtilitiesTest()
        {
            efService = new Mock<IEntityFrameworkService>();
            modelTypesLocator = new Mock<IModelTypesLocator>();
            modelTypesLocatorWithoutContext = new Mock<IModelTypesLocator>();
            codeModelService = new Mock<ICodeModelService>();
            logger= new Mock<ILogger>();
        }

        [Fact]
        public async void TestValidateModelAndGetCodeModelMetadata()
        {
            var modelTypes = new List<ModelType>();
            //Arrange
            model = new TestCommandLineModel()
            {
                ModelClass = "InvalidModel"
            };
            modelTypesLocator.Setup(m => m.GetType("InvalidModel")).Returns(() => { return modelTypes; });

            //Act & Assert
            Exception ex = await Assert.ThrowsAsync<ArgumentException>(
                async () => await ModelMetadataUtilities.ValidateModelAndGetCodeModelMetadata(
                    model,
                    codeModelService.Object,
                    modelTypesLocator.Object));
            Assert.Equal("A type with the name InvalidModel does not exist", ex.Message);

            // Arrange
            var modelType = new ModelType()
            {
                Name = "InvalidModel",
                FullName = "InvalidModel",
                Namespace = ""
            };
            modelTypes.Add(modelType);
            var contextProcessingResult = new ContextProcessingResult()
            {
                ContextProcessingStatus = ContextProcessingStatus.ContextAdded,
                ModelMetadata = null
            };

            codeModelService.Setup(e => e.GetModelMetadata(modelType))
                .Returns(Task.FromResult(contextProcessingResult));

            //Act
            var result = await ModelMetadataUtilities.ValidateModelAndGetCodeModelMetadata(
                    model,
                    codeModelService.Object,
                    modelTypesLocator.Object);

            //Assert
            Assert.Equal(contextProcessingResult.ContextProcessingStatus, result.ContextProcessingResult.ContextProcessingStatus);
            Assert.Equal(modelType, result.ModelType);
        }

        [Fact]
        public async void TestValidateModelAndGetModelMetadata()
        {
            var modelTypes = new List<ModelType>();
            var dataContextTypes = new List<ModelType>();
            //Arrange
            model = new TestCommandLineModel()
            {
                ModelClass = "SampleModel",
                DataContextClass = "SampleDataContext"
            };
            modelTypesLocator.Setup(m => m.GetType("SampleModel")).Returns(() => { return modelTypes; });
            modelTypesLocator.Setup(m => m.GetType("SampleDataContext")).Returns(() => { return dataContextTypes; });

            //Act & Assert
            Exception ex = await Assert.ThrowsAsync<ArgumentException>(
                async () => await ModelMetadataUtilities.ValidateModelAndGetCodeModelMetadata(
                    model,
                    codeModelService.Object,
                    modelTypesLocator.Object));
            Assert.Equal("A type with the name SampleModel does not exist", ex.Message);

            // Arrange
            var modelType = new ModelType()
            {
                Name = "SampleModel",
                FullName = "SampleModel",
                Namespace = ""
            };

            modelTypes.Add(modelType);
            var contextProcessingResult = new ContextProcessingResult()
            {
                ContextProcessingStatus = ContextProcessingStatus.ContextAdded,
                ModelMetadata = null
            };

            efService.Setup(e => e.GetModelMetadata(model.DataContextClass, modelType, string.Empty, DbProvider.SqlServer))
                .Returns(Task.FromResult(contextProcessingResult));

            //Act
            var result = await ModelMetadataUtilities.ValidateModelAndGetEFMetadata(
                    model,
                    efService.Object,
                    modelTypesLocator.Object,
                    logger.Object,
                    string.Empty);

            //Assert
            Assert.Equal(contextProcessingResult.ContextProcessingStatus, result.ContextProcessingResult.ContextProcessingStatus);
            Assert.Equal(modelType, result.ModelType);
            Assert.Equal(model.DataContextClass, result.DbContextFullName);

            // Arrange
            var dataContextType = new ModelType()
            {
                Name = "SampleDatContext",
                FullName = "A.B.C.SampleDataContext"
            };
            dataContextTypes.Add(dataContextType);
            efService.Setup(e => e.GetModelMetadata(dataContextType.FullName, modelType, string.Empty, DbProvider.SqlServer))
                .Returns(Task.FromResult(contextProcessingResult));

            //Act
            result = await ModelMetadataUtilities.ValidateModelAndGetEFMetadata(
                    model,
                    efService.Object,
                    modelTypesLocator.Object,
                    logger.Object,
                    string.Empty);

            //Assert
            Assert.Equal(contextProcessingResult.ContextProcessingStatus, result.ContextProcessingResult.ContextProcessingStatus);
            Assert.Equal(modelType, result.ModelType);
            Assert.Equal(dataContextType.FullName, result.DbContextFullName);
        }

        [Fact]
        public async void TestGetModelEFMetadataMinimalAsync()
        {
            var modelTypes = new List<ModelType>();
            var dataContextTypes = new List<ModelType>();
            //Arrange
            MinimalApiGeneratorCommandLineModel minimalApiModelWithContext = new MinimalApiGeneratorCommandLineModel()
            {
                ModelClass = "SampleModel",
                DataContextClass = "SampleDataContext"
            };

            MinimalApiGeneratorCommandLineModel minimalApiModelWithoutContext = new MinimalApiGeneratorCommandLineModel()
            {
                ModelClass = "SampleModel",
                DataContextClass = string.Empty
            };

            modelTypesLocator.Setup(m => m.GetType("SampleModel")).Returns(() => { return modelTypes; });
            modelTypesLocator.Setup(m => m.GetType("SampleDataContext")).Returns(() => { return dataContextTypes; });

            modelTypesLocatorWithoutContext.Setup(m => m.GetType("SampleModel")).Returns(() => { return modelTypes; });

            //Act & Assert
            Exception ex = await Assert.ThrowsAsync<ArgumentException>(
                async () => await ModelMetadataUtilities.GetModelEFMetadataMinimalAsync(
                    minimalApiModelWithContext,
                    efService.Object,
                    modelTypesLocator.Object,
                    logger.Object,
                    areaName: string.Empty));

            Exception exWithoutContext = await Assert.ThrowsAsync<ArgumentException>(
                async () => await ModelMetadataUtilities.GetModelEFMetadataMinimalAsync(
                    minimalApiModelWithoutContext,
                    efService.Object,
                    modelTypesLocatorWithoutContext.Object,
                    logger.Object,
                    areaName: string.Empty));

            Assert.Equal("A type with the name SampleModel does not exist", ex.Message);
            Assert.Equal("A type with the name SampleModel does not exist", exWithoutContext.Message);

            // Arrange
            var modelType = new ModelType()
            {
                Name = "SampleModel",
                FullName = "SampleModel",
                Namespace = ""
            };

            modelTypes.Add(modelType);
            var contextProcessingResult = new ContextProcessingResult()
            {
                ContextProcessingStatus = ContextProcessingStatus.ContextAdded,
                ModelMetadata = null
            };


            var noContext = new ContextProcessingResult()
            {
                ContextProcessingStatus = ContextProcessingStatus.MissingContext,
                ModelMetadata = null
            };

            efService.Setup(e => e.GetModelMetadata(minimalApiModelWithContext.DataContextClass, modelType, string.Empty, DbProvider.SqlServer))
                .Returns(Task.FromResult(contextProcessingResult));

            //Act
            var result = await ModelMetadataUtilities.GetModelEFMetadataMinimalAsync(
                    minimalApiModelWithContext,
                    efService.Object,
                    modelTypesLocator.Object,
                    logger.Object,
                    string.Empty);

            var resultWithoutContext = await ModelMetadataUtilities.GetModelEFMetadataMinimalAsync(
                minimalApiModelWithoutContext,
                efService.Object,
                modelTypesLocatorWithoutContext.Object,
                logger.Object,
                string.Empty);

            //Assert scenario with DbContext
            Assert.Equal(contextProcessingResult.ContextProcessingStatus, result.ContextProcessingResult.ContextProcessingStatus);
            Assert.Equal(modelType, result.ModelType);
            Assert.Equal(minimalApiModelWithContext.DataContextClass, result.DbContextFullName);

            //Assert scenario without DbContext
            Assert.Equal(noContext.ContextProcessingStatus, resultWithoutContext.ContextProcessingResult.ContextProcessingStatus);
            Assert.Equal(modelType, resultWithoutContext.ModelType);
            Assert.Equal(minimalApiModelWithoutContext.DataContextClass, string.Empty);

            // Arrange
            var dataContextType = new ModelType()
            {
                Name = "SampleDatContext",
                FullName = "A.B.C.SampleDataContext"
            };
            dataContextTypes.Add(dataContextType);
            efService.Setup(e => e.GetModelMetadata(dataContextType.FullName, modelType, string.Empty, DbProvider.SqlServer))
                .Returns(Task.FromResult(contextProcessingResult));

            //Act
            result = await ModelMetadataUtilities.GetModelEFMetadataMinimalAsync(
                minimalApiModelWithContext,
                efService.Object,
                modelTypesLocator.Object,
                logger.Object,
                string.Empty);

            //Assert
            Assert.Equal(contextProcessingResult.ContextProcessingStatus, result.ContextProcessingResult.ContextProcessingStatus);
            Assert.Equal(modelType, result.ModelType);
            Assert.Equal(dataContextType.FullName, result.DbContextFullName);
        }

        class TestCommandLineModel : CommonCommandLineModel
        {
            public override CommonCommandLineModel Clone()
            {
                return new TestCommandLineModel();
            }
        }
    }
}
