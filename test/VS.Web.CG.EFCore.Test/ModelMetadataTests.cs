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
            Assert.Single(productMetadata.Navigations);

            //Arrange
            var categoryEntity = TestModel.CategoryProductModel.FindEntityType(typeof(Category));
            var categoryMetadata = new ModelMetadata(categoryEntity, typeof(TestDbContext));

            //Act && Assert
            Assert.Equal(2, categoryMetadata.Properties.Length);
            Assert.Empty(categoryMetadata.Navigations);

            //Arrange
            var customerEntity = TestModel.CustomerOrderModel.FindEntityType(typeof(Customer));
            var customerMetadata = new ModelMetadata(customerEntity, typeof(TestDbContext));

            //Act && Assert
            Assert.Equal(2, customerMetadata.Properties.Length);
            Assert.Empty(customerMetadata.Navigations);

            //Arrange
            var orderEntity = TestModel.CustomerOrderModel.FindEntityType(typeof(Order));
            var orderMetadata = new ModelMetadata(orderEntity, typeof(TestDbContext));

            //Act && Assert
            Assert.Equal(3, orderMetadata.Properties.Length);
            Assert.Single(orderMetadata.Navigations);
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
            Assert.Single(primaryKeys);
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
            Assert.Equal("ProductId", properties[0].PropertyName);
            Assert.Equal("ProductName", properties[1].PropertyName);
            Assert.Equal("CategoryId", properties[2].PropertyName);
            Assert.Equal("EnumProperty", properties[3].PropertyName);
        }
    }
}
