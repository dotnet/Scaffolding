// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using Xunit;

namespace Microsoft.VisualStudio.Web.CodeGeneration.EntityFrameworkCore.Test
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
