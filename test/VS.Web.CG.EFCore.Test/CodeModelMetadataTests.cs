// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
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
