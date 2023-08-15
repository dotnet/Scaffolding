// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;

namespace Microsoft.DotNet.Scaffolding.Shared.Tests
{
    public class StringUtilTests
    {
        [Fact]
        public void ContainsIgnoreCase_ShouldReturnCorrectResults()
        {
            // Arrange
            string input1 = "Hello";
            string input2 = "World";
            string input3 = "";
            string input4 = "";
            string input5 = "test";
            string input6 = "Hello123";
            string input7 = "123$%#Hello";
            string input8 = null;

            string value1 = "hello";
            string value2 = "word";
            string value3 = string.Empty;
            string value4 = "test";
            string value5 = "";
            string value6 = "hello123";
            string value7 = "123$%#hello";
            string value8 = null;

            // Act and Assert
            Assert.True(input1.ContainsIgnoreCase(value1));
            Assert.False(input2.ContainsIgnoreCase(value2));
            Assert.True(input3.ContainsIgnoreCase(value3));
            Assert.False(input4.ContainsIgnoreCase(value4));
            Assert.False(input5.ContainsIgnoreCase(value5));
            Assert.True(input6.ContainsIgnoreCase(value6));
            Assert.True(input7.ContainsIgnoreCase(value7));
            //null.ContainsIgnoreCase(string) should return false
            Assert.False(input8.ContainsIgnoreCase(value7));
            //null.ContainsIgnoreCase(null) should return false
            Assert.False(input8.ContainsIgnoreCase(value8));
            //string.ContainsIgnoreCase(null) should return false
            Assert.False(input7.ContainsIgnoreCase(value8));
        }
    }
}
