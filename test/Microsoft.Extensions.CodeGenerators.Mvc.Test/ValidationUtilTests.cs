// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Xunit;

namespace Microsoft.Extensions.CodeGenerators.Mvc.Test
{
    public class ValidationUtilTests
    {
        [Theory]
        [InlineData(null)]
        [InlineData("")]
        public void ValidateType_Throws_When_TypeName_Is_Null_Or_Empty(string typeName)
        {
            var ex = Assert.Throws<ArgumentException>(() => ValidationUtil.ValidateType(typeName, "parameterName", modelTypesLocator:null));
            Assert.Equal(@"Please provide a valid parameterName", ex.Message);
        }
    }
}