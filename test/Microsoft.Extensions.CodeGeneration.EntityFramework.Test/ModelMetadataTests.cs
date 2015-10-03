// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.Extensions.CodeGeneration.EntityFramework.Test.TestModels;
using Xunit;

namespace Microsoft.Extensions.CodeGeneration.EntityFramework.Test
{
    public class ModelMetadataTests
    {
        [Fact]
        public void Number_Of_Properties_Is_Correct()
        {
            //Arrange
            var productEntity = TestModel.Model.GetEntityType(typeof(Product));
            var modelMetadata = new ModelMetadata(productEntity, typeof(TestDbContext));

            //Act
            var properties = modelMetadata.Properties;

            //Assert
            Assert.Equal(4, properties.Length);
        }

        [Fact]
        public void PrimaryKeys_Are_Returned_Correct()
        {
            //Arrange
            var productEntity = TestModel.Model.GetEntityType(typeof(Product));
            var modelMetadata = new ModelMetadata(productEntity, typeof(TestDbContext));

            //Act
            var primaryKeys = modelMetadata.PrimaryKeys;

            //Assert
            Assert.Equal(1, primaryKeys.Length);
            var primaryKey = primaryKeys[0];
            Assert.Equal("ProductId", primaryKey.PropertyName);
        }

        [Fact]
        public void EntitySetName_Uses_DbContext_PropertyName()
        {
            //Arrange
            var categoryEntity = TestModel.Model.GetEntityType(typeof(Category));
            var modelMetadata = new ModelMetadata(categoryEntity, typeof(TestDbContext));

            //Act
            var entitySetName = modelMetadata.EntitySetName;

            //Assert
            Assert.Equal("Categories", entitySetName);
        }

        [Fact]
        public void EntitySetName_Fallsback_To_GenericSetMethod()
        {
            //Arrange
            var productEntity = TestModel.Model.GetEntityType(typeof(Product));
            var modelMetadata = new ModelMetadata(productEntity, typeof(TestDbContext));

            //Act
            var entitySetName = modelMetadata.EntitySetName;

            //Assert
            Assert.Equal("Set<Product>()", entitySetName);
        }
    }
}