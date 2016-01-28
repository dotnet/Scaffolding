// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.Extensions.CodeGeneration.CommandLine;
using Xunit;

namespace Microsoft.Extensions.CodeGeneration
{
    public class MemberInfoExtensionsTests
    {
        [Fact]
        public void GetAliasAttribute_Returns_Correct_Value()
        {
            //TODO @prbhosal Fix this test
            ////Arrange
            //MemberInfo testType = typeof(ClassWithAlias);

            ////Act
            //var attribute = testType.GetAliasAttribute();

            ////Assert
            //Assert.Equal("CoolClass", attribute.Alias);
        }

        [Fact]
        public void GetOptionAttribute_Returns_Correct_Value()
        {
            //Arrange
            var testProp = typeof(ClassWithAlias).GetProperty("PropertyWithOptionAttribute");

            //Act
            var attribute = testProp.GetOptionAttribute();

            //Assert
            Assert.Equal("Awesome", attribute.Name);
            Assert.Equal("aw", attribute.ShortName);
            Assert.Equal("Awesome Property", attribute.Description);
            Assert.Equal("Default", attribute.DefaultValue);
        }

        [Fact]
        public void GetArgumentAttribute_Returns_Correct_Value()
        {
            //Arrange
            var testProp = typeof(ClassWithAlias).GetProperty("PropertyWithArgumentAttribute");

            //Act
            var attribute = testProp.GetArgumentAttribute();

            //Assert
            Assert.Equal("Awesome argument", attribute.Description);
        }

        [Alias("CoolClass")]
        private class ClassWithAlias
        {
            [Option(Description = "Awesome Property", Name = "Awesome", ShortName = "aw", DefaultValue = "Default")]
            public bool PropertyWithOptionAttribute { get; set; }

            [Argument(Description = "Awesome argument")]
            public string PropertyWithArgumentAttribute { get; set; }
        }
    }
}