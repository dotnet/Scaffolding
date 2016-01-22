// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Xunit;

namespace Microsoft.Extensions.CodeGeneration.EntityFramework.Test
{
    public class TypeUtilTests
    {
        [Theory]
        [InlineData(typeof(TypeUtil), "TypeUtil")]
        [InlineData(typeof(int), "int")]
        [InlineData(typeof(short), "short")]
        [InlineData(typeof(int?), "int?")]
        [InlineData(typeof(List<int>), "List<int>")]
        [InlineData(typeof(List<int?>), "List<int?>")]
        [InlineData(typeof(List<List<int?>>), "List<List<int?>>")]
        [InlineData(typeof(Dictionary<int?, short?>), "Dictionary<int?, short?>")]
        [InlineData(typeof(Dictionary<int, short?>), "Dictionary<int, short?>")]
        public void GetShortTypeName_Returns_Friendly_Short_Name(Type type, string expected)
        {
            var actual = TypeUtil.GetShortTypeName(type);
            Assert.Equal(expected, actual);
        }
    }
}
