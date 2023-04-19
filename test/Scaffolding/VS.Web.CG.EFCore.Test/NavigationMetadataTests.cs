// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.Web.CodeGeneration.EntityFrameworkCore.Test.TestModels;
using Xunit;
//using Xunit.SkippableFact;

namespace Microsoft.VisualStudio.Web.CodeGeneration.EntityFrameworkCore.Test
{
    public class NavigationMetadataTests
    {
        [SkippableFact]
        public void ManyToOneRelation_Metadata_Is_Correct()
        {
            string runSkippableTests = Environment.GetEnvironmentVariable("SCAFFOLDING_RunSkippableTests");
            Skip.If(string.IsNullOrEmpty(runSkippableTests));

            //Arrange
            var productEntity = TestModel.CategoryProductModel.FindEntityType(typeof(Product));
            var modelMetadata = new ModelMetadata(productEntity, typeof(TestDbContext));

            //Act
            var navigationMetadata = modelMetadata.Navigations.FirstOrDefault(p => p.AssociationPropertyName == nameof(Product.Category));

            //Assert
            Assert.NotNull(navigationMetadata);
            Assert.Equal(nameof(Product.Category), navigationMetadata.AssociationPropertyName);
            Assert.Equal(nameof(TestDbContext.Categories), navigationMetadata.EntitySetName);
            Assert.Equal(nameof(Category.CategoryName), navigationMetadata.DisplayPropertyName);
            Assert.Single(navigationMetadata.ForeignKeyPropertyNames);
            Assert.Equal(nameof(Product.CategoryId), navigationMetadata.ForeignKeyPropertyNames[0]);
            Assert.Single(navigationMetadata.PrimaryKeyNames);
            Assert.Equal(nameof(Category.CategoryId), navigationMetadata.PrimaryKeyNames[0]);
            Assert.Equal(typeof(Category).FullName, navigationMetadata.TypeName);
            Assert.Equal(typeof(Category).Name, navigationMetadata.ShortTypeName);
        }

        [SkippableFact]
        public void ForeignKeyRelation_Metadata_Is_Correct()
        {
            string runSkippableTests = Environment.GetEnvironmentVariable("SCAFFOLDING_RunSkippableTests");
            Skip.If(string.IsNullOrEmpty(runSkippableTests));

            //Arrange
            var orderEntity = TestModel.CustomerOrderModel.FindEntityType(typeof(Order));
            var modelMetadata = new ModelMetadata(orderEntity, typeof(TestDbContext));

            //Act
            var navigationMetadata = modelMetadata.Navigations.FirstOrDefault(p => p.AssociationPropertyName == nameof(Order.Customer));

            //Assert
            Assert.NotNull(navigationMetadata);
            Assert.Equal(nameof(Order.Customer), navigationMetadata.AssociationPropertyName);
            Assert.Equal(nameof(TestDbContext.Customers), navigationMetadata.EntitySetName);
            Assert.Equal(nameof(Customer.CustomerId), navigationMetadata.DisplayPropertyName); //Todo
            Assert.Single(navigationMetadata.ForeignKeyPropertyNames);
            Assert.Equal(nameof(Order.CustomerId), navigationMetadata.ForeignKeyPropertyNames[0]);
            Assert.Single(navigationMetadata.PrimaryKeyNames);
            Assert.Equal(nameof(Customer.CustomerId), navigationMetadata.PrimaryKeyNames[0]);
            Assert.Equal(typeof(Customer).FullName, navigationMetadata.TypeName);
            Assert.Equal(typeof(Customer).Name, navigationMetadata.ShortTypeName);
        }

        [SkippableFact]
        public void OneToManyNavigation_Excluded_From_Metadata()
        {
            string runSkippableTests = Environment.GetEnvironmentVariable("SCAFFOLDING_RunSkippableTests");
            Skip.If(string.IsNullOrEmpty(runSkippableTests));

            //Arrange
            var categoryEntity = TestModel.CategoryProductModel.FindEntityType(typeof(Category));
            var modelMetadata = new ModelMetadata(categoryEntity, typeof(TestDbContext));

            //Act
            var navigationMetadata = modelMetadata.Navigations.FirstOrDefault(p => p.AssociationPropertyName == nameof(Category.Products));

            //Assert
            Assert.Null(navigationMetadata);
        }

        [SkippableFact]
        public void Independent_Association_Excluded_From_Metadata()
        {
            string runSkippableTests = Environment.GetEnvironmentVariable("SCAFFOLDING_RunSkippableTests");
            Skip.If(string.IsNullOrEmpty(runSkippableTests));

            //Arrange
            var customerEntity = TestModel.CustomerOrderModel.FindEntityType(typeof(Customer));
            var modelMetadata = new ModelMetadata(customerEntity, typeof(TestDbContext));

            //Act
            var navigationMetadata = modelMetadata.Navigations.FirstOrDefault(p => p.AssociationPropertyName == nameof(Customer.CustomerDetails));

            //Assert
            Assert.Null(navigationMetadata);
        }
    }
}
