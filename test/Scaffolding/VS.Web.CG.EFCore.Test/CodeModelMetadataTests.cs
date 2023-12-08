// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Linq;
using Microsoft.VisualStudio.Web.CodeGeneration.EntityFrameworkCore.Test.TestModels;
using Xunit;

namespace Microsoft.VisualStudio.Web.CodeGeneration.EntityFrameworkCore.Test
{
    public class CodeModelMetadataTests
    {
        [Fact]
        public void Number_Of_Properties_And_Navigations_Is_Correct()
        {
            //Arrange
            var metadata = new CodeModelMetadata(typeof(Product));

            //Act && Assert
            Assert.Equal(9, metadata.Properties.Length);
            Assert.Empty(metadata.PrimaryKeys);
            Assert.Empty(metadata.Navigations);
            //Arrange
            metadata = new CodeModelMetadata(typeof(Category));

            //Act && Assert
            Assert.Equal(2, metadata.Properties.Length);
            Assert.Empty(metadata.PrimaryKeys);
            Assert.Empty(metadata.Navigations);

            //Arrange
            metadata = new CodeModelMetadata(typeof(Customer));

            //Act && Assert
            Assert.Equal(2, metadata.Properties.Length);
            Assert.Empty(metadata.PrimaryKeys);
            Assert.Empty(metadata.Navigations);
        }
    }
}
