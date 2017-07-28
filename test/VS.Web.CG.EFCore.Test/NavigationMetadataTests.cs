// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Linq;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.Web.CodeGeneration.EntityFrameworkCore.Test.TestModels;
using Xunit;

namespace Microsoft.VisualStudio.Web.CodeGeneration.EntityFrameworkCore.Test
{
    public class NavigationMetadataTests
    {
        [Fact]
        public void ManyToOneRelation_Metadata_Is_Correct()
        {
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
            Assert.Equal(1, navigationMetadata.ForeignKeyPropertyNames.Count());
            Assert.Equal(nameof(Product.CategoryId), navigationMetadata.ForeignKeyPropertyNames[0]);
            Assert.Equal(1, navigationMetadata.PrimaryKeyNames.Count());
            Assert.Equal(nameof(Category.CategoryId), navigationMetadata.PrimaryKeyNames[0]);
            Assert.Equal(typeof(Category).FullName, navigationMetadata.TypeName);
            Assert.Equal(typeof(Category).Name, navigationMetadata.ShortTypeName);
        }

        [Fact]
        public void ForiengKeyRelation_Metadata_Is_Correct()
        {
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
            Assert.Equal(1, navigationMetadata.ForeignKeyPropertyNames.Count());
            Assert.Equal(nameof(Order.CustomerId), navigationMetadata.ForeignKeyPropertyNames[0]);
            Assert.Equal(1, navigationMetadata.PrimaryKeyNames.Count());
            Assert.Equal(nameof(Customer.CustomerId), navigationMetadata.PrimaryKeyNames[0]);
            Assert.Equal(typeof(Customer).FullName, navigationMetadata.TypeName);
            Assert.Equal(typeof(Customer).Name, navigationMetadata.ShortTypeName);
        }

        [Fact]
        public void OneToManyNavigation_Excluded_From_Metadata()
        {
            //Arrange
            var categoryEntity = TestModel.CategoryProductModel.FindEntityType(typeof(Category));
            var modelMetadata = new ModelMetadata(categoryEntity, typeof(TestDbContext));

            //Act
            var navigationMetadata = modelMetadata.Navigations.FirstOrDefault(p => p.AssociationPropertyName == nameof(Category.Products));

            //Assert
            Assert.Null(navigationMetadata);
        }

        [Fact]
        public void Independent_Association_Excluded_From_Metadata()
        {
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
