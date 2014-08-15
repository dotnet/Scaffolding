// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.Framework.CodeGeneration.EntityFramework.Test.TestModels;
using Xunit;

namespace Microsoft.Framework.CodeGeneration.EntityFramework.Test
{
    public class PropertyMetadataTests
    {
        [Fact]
        public void Primary_Key_Metadata_Is_Correct()
        {
            //Arrange
            var productEntity = TestModel.Model.GetEntityType(typeof(Product));
            var productIdProperty = productEntity.GetProperty("ProductId");

            //Act
            var propertyMetadata = new PropertyMetadata(productIdProperty);

            //Assert
            Assert.Equal("ProductId", propertyMetadata.PropertyName);
            Assert.Equal(true, propertyMetadata.IsPrimaryKey);
            Assert.Equal(false, propertyMetadata.IsForeignKey);
            Assert.Equal(typeof(int).FullName, propertyMetadata.TypeName);
            Assert.Equal(false, propertyMetadata.IsEnum);
        }

        [Fact]
        public void Foreign_Key_Metadata_Is_Correct()
        {
            //Arrange
            var productEntity = TestModel.Model.GetEntityType(typeof(Product));
            var productCategoryIdProperty = productEntity.GetProperty("ProductCategoryId");

            //Act
            var propertyMetadata = new PropertyMetadata(productCategoryIdProperty);

            //Assert
            Assert.Equal("ProductCategoryId", propertyMetadata.PropertyName);
            Assert.Equal(false, propertyMetadata.IsPrimaryKey);
            Assert.Equal(true, propertyMetadata.IsForeignKey);
            Assert.Equal(typeof(int).FullName, propertyMetadata.TypeName);
            Assert.Equal(false, propertyMetadata.IsEnum);
        }

        [Fact]
        public void String_Property_Metadata_Is_Correct()
        {
            //Arrange
            var productEntity = TestModel.Model.GetEntityType(typeof(Product));
            var productNameProperty = productEntity.GetProperty("ProductName");

            //Act
            var propertyMetadata = new PropertyMetadata(productNameProperty);

            //Assert
            Assert.Equal("ProductName", propertyMetadata.PropertyName);
            Assert.Equal(false, propertyMetadata.IsPrimaryKey);
            Assert.Equal(false, propertyMetadata.IsForeignKey);
            Assert.Equal(typeof(string).FullName, propertyMetadata.TypeName);
            Assert.Equal(false, propertyMetadata.IsEnum);
        }

        [Fact]
        public void Enum_Property_Metadata_Is_Correct()
        {
            //Arrange
            var productEntity = TestModel.Model.GetEntityType(typeof(Product));
            var productEnumProperty = productEntity.GetProperty("ProductEnumProperty");

            //Act
            var propertyMetadata = new PropertyMetadata(productEnumProperty);

            //Assert
            Assert.Equal("ProductEnumProperty", propertyMetadata.PropertyName);
            Assert.Equal(false, propertyMetadata.IsPrimaryKey);
            Assert.Equal(false, propertyMetadata.IsForeignKey);
            Assert.Equal(typeof(EnumType).FullName, propertyMetadata.TypeName);
            Assert.Equal(true, propertyMetadata.IsEnum);
        }
    }
}