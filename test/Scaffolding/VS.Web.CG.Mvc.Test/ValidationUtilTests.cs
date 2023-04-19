// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Xunit;

namespace Microsoft.VisualStudio.Web.CodeGenerators.Mvc.Test
{
    public class ValidationUtilTests
    {
        [Theory]
        [InlineData(null)]
        [InlineData("")]
        public void ValidateType_Throws_When_TypeName_Is_Null_Or_Empty(string typeName)
        {
            var ex = Assert.Throws<ArgumentException>(() => ValidationUtil.ValidateType(typeName, "parameterName", modelTypesLocator: null));
            Assert.Equal(@"Provide a valid parameterName", ex.Message);
        }
    }
}
