// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.VisualStudio.Web.CodeGeneration.EntityFrameworkCore.Test.TestModels;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Microsoft.VisualStudio.Web.CodeGeneration.EntityFrameworkCore.Test
{
    public class ModelMetadataTests
    {
        [Fact]
        public void Number_Of_Properties_And_Navigations_Is_Correct()
        {
            //Arrange
            var productEntity = TestModel.CategoryProductModel.FindEntityType(typeof(Product));
            var productMetadata = new ModelMetadata(productEntity, typeof(TestDbContext));

            //Act && Assert
            Assert.Equal(8, productMetadata.Properties.Length);
            Assert.Equal(1, productMetadata.Navigations.Length);

            //Arrange
            var categoryEntity = TestModel.CategoryProductModel.FindEntityType(typeof(Category));
            var categoryMetadata = new ModelMetadata(categoryEntity, typeof(TestDbContext));

            //Act && Assert
            Assert.Equal(2, categoryMetadata.Properties.Length);
            Assert.Equal(0, categoryMetadata.Navigations.Length);

            //Arrange
            var customerEntity = TestModel.CustomerOrderModel.FindEntityType(typeof(Customer));
            var customerMetadata = new ModelMetadata(customerEntity, typeof(TestDbContext));

            //Act && Assert
            Assert.Equal(2, customerMetadata.Properties.Length);
            Assert.Equal(0, customerMetadata.Navigations.Length);

            //Arrange
            var orderEntity = TestModel.CustomerOrderModel.FindEntityType(typeof(Order));
            var orderMetadata = new ModelMetadata(orderEntity, typeof(TestDbContext));

            //Act && Assert
            Assert.Equal(3, orderMetadata.Properties.Length);
            Assert.Equal(1, orderMetadata.Navigations.Length);
        }

        [Fact]
        public void PrimaryKeys_Are_Returned_Correct()
        {
            //Arrange
            var productEntity = TestModel.CategoryProductModel.FindEntityType(typeof(Product));
            var modelMetadata = new ModelMetadata(productEntity, typeof(TestDbContext));

            //Act
            var primaryKeys = modelMetadata.PrimaryKeys;

            //Assert
            Assert.Equal(1, primaryKeys.Length);
            var primaryKey = primaryKeys[0];
            Assert.Equal(nameof(Product.ProductId), primaryKey.PropertyName);
        }

        [Fact]
        public void EntitySetName_Uses_DbContext_PropertyName()
        {
            //Arrange
            var categoryEntity = TestModel.CategoryProductModel.FindEntityType(typeof(Category));
            var modelMetadata = new ModelMetadata(categoryEntity, typeof(TestDbContext));

            //Act
            var entitySetName = modelMetadata.EntitySetName;

            //Assert
            Assert.Equal(nameof(TestDbContext.Categories), entitySetName);
        }

        [Fact]
        public void EntitySetName_Fallsback_To_GenericSetMethod()
        {
            //Arrange
            var productEntity = TestModel.CategoryProductModel.FindEntityType(typeof(Product));
            var modelMetadata = new ModelMetadata(productEntity, typeof(TestDbContext));

            //Act
            var entitySetName = modelMetadata.EntitySetName;

            //Assert
            Assert.Equal("Set<Product>()", entitySetName);
        }

        [Fact]
        public void Properties_Are_Sorted_According_To_Reflection_order()
        {
            // Arrange
            var productEntity = TestModel.CategoryProductModel.FindEntityType(typeof(Product));
            var modelMetadata = new ModelMetadata(productEntity, typeof(TestDbContext));

            // Act
            var properties = modelMetadata.Properties;

            // Assert
            Assert.Equal(properties[0].PropertyName, "ProductId");
            Assert.Equal(properties[1].PropertyName, "ProductName");
            Assert.Equal(properties[2].PropertyName, "CategoryId");
            Assert.Equal(properties[3].PropertyName, "EnumProperty");
        }
    }
}