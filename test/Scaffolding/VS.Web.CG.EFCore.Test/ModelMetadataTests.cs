// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.VisualStudio.Web.CodeGeneration.EntityFrameworkCore.Test.TestModels;
using Microsoft.EntityFrameworkCore;
using Xunit;
using System;

namespace Microsoft.VisualStudio.Web.CodeGeneration.EntityFrameworkCore.Test
{
    public class ModelMetadataTests
    {
        [SkippableFact]
        public void Number_Of_Properties_And_Navigations_Is_Correct()
        {
            string runSkippableTests = Environment.GetEnvironmentVariable("SCAFFOLDING_RunSkippableTests");
            Skip.If(string.IsNullOrEmpty(runSkippableTests));

            //Arrange
            var productEntity = TestModel.CategoryProductModel.FindEntityType(typeof(Product));
            var productMetadata = new ModelMetadata(productEntity, typeof(TestDbContext));

            //Act && Assert
            Assert.Equal(9, productMetadata.Properties.Length);
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

        [SkippableFact]
        public void PrimaryKeys_Are_Returned_Correct()
        {
            string runSkippableTests = Environment.GetEnvironmentVariable("SCAFFOLDING_RunSkippableTests");
            Skip.If(string.IsNullOrEmpty(runSkippableTests));

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

        [SkippableFact]
        public void EntitySetName_Uses_DbContext_PropertyName()
        {
            string runSkippableTests = Environment.GetEnvironmentVariable("SCAFFOLDING_RunSkippableTests");
            Skip.If(string.IsNullOrEmpty(runSkippableTests));

            //Arrange
            var categoryEntity = TestModel.CategoryProductModel.FindEntityType(typeof(Category));
            var modelMetadata = new ModelMetadata(categoryEntity, typeof(TestDbContext));

            //Act
            var entitySetName = modelMetadata.EntitySetName;

            //Assert
            Assert.Equal(nameof(TestDbContext.Categories), entitySetName);
        }

        [SkippableFact]
        public void EntitySetName_Fallsback_To_GenericSetMethod()
        {
            string runSkippableTests = Environment.GetEnvironmentVariable("SCAFFOLDING_RunSkippableTests");
            Skip.If(string.IsNullOrEmpty(runSkippableTests));

            //Arrange
            var productEntity = TestModel.CategoryProductModel.FindEntityType(typeof(Product));
            var modelMetadata = new ModelMetadata(productEntity, typeof(TestDbContext));

            //Act
            var entitySetName = modelMetadata.EntitySetName;

            //Assert
            Assert.Equal("Set<Product>()", entitySetName);
        }

        [SkippableFact]
        public void Properties_Are_Sorted_According_To_Reflection_order()
        {
            string runSkippableTests = Environment.GetEnvironmentVariable("SCAFFOLDING_RunSkippableTests");
            Skip.If(string.IsNullOrEmpty(runSkippableTests));

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
