using System.Collections.Generic;
using System.Linq;
using Microsoft.Data.Entity.Metadata;
using Microsoft.Extensions.CodeGeneration.EntityFramework.Test.TestModels;
using Xunit;

namespace Microsoft.Extensions.CodeGeneration.EntityFramework.Test
{
    public class NavigationMetadataTests
    {
        [Fact]
        public void ManyToOneRelation_Metadata_Is_Correct()
        {
            //Arrange
            var productEntity = TestModel.CategoryProductModel.GetEntityType(typeof(Product));
            var modelMetadata = new ModelMetadata(productEntity, typeof(TestDbContext));

            //Act
            var navigationMetadata = modelMetadata.Navigations.FirstOrDefault(p => p.AssociationPropertyName == nameof(Product.Category));

            //Assert
            Assert.NotNull(navigationMetadata);
            Assert.Equal("Category", navigationMetadata.AssociationPropertyName);
            Assert.Equal("Categories", navigationMetadata.EntitySetName);
            Assert.Equal("Category", navigationMetadata.DisplayPropertyName); //Todo
            Assert.Equal(1, navigationMetadata.FoeignKeyPropertyNames.Count());
            Assert.Equal("CategoryId", navigationMetadata.FoeignKeyPropertyNames[0]);
            Assert.Equal(1, navigationMetadata.PrimaryKeyNames.Count());
            Assert.Equal("CategoryId", navigationMetadata.PrimaryKeyNames[0]);
            Assert.Equal(typeof(Category).FullName, navigationMetadata.TypeName);
            Assert.Equal(typeof(Category).Name, navigationMetadata.ShortTypeName);
        }

        [Fact]
        public void OneToManyNavigation_Excluded_From_Metadata()
        {
            //Arrange
            var categoryEntity = TestModel.CategoryProductModel.GetEntityType(typeof(Category));
            var modelMetadata = new ModelMetadata(categoryEntity, typeof(TestDbContext));

            //Act
            var navigationMetadata = modelMetadata.Navigations.FirstOrDefault(p => p.AssociationPropertyName == nameof(Category.Products));

            //Assert
            Assert.Null(navigationMetadata);
        }
    }
}
