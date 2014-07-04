// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Linq;
using Microsoft.Framework.CodeGeneration.CommandLine;
using Microsoft.Framework.Runtime.Common.CommandLine;
using Xunit;

namespace Microsoft.Framework.CodeGeneration.Core.Test
{
    public class ParameterDescriptorTests
    {
        [Theory]
        [InlineData("BooleanWithoutExplicitOption",
                    "--BooleanWithoutExplicitOption",
                    "",
                    "--BooleanWithoutExplicitOption",
                    false)]
        [InlineData("BooleanWithExplicitOption",
                    "--NameOverride|-bwo",
                    "Bool with explicit option",
                    "--NameOverride",
                    false)]
        [InlineData("BooleanWithDefaultValue",
                    "--BooleanWithDefaultValue",
                    "",
                    "--BooleanWithDefaultValue",
                    true)]
        public void Boolean_Options_UseCorrectName_Returns_CorrectValue(
            string propertyName,
            string expectedOptionTemplate,
            string expectedOptionDescription,
            string trueValuTestCase,
            bool falseTestDefaultValue)
        {
            //Arrange
            var command = new CommandLineApplication();
            var property = typeof(TestClass).GetProperty(propertyName);
            var descriptor = new ParameterDescriptor(property);
            var expectedOption = new CommandOption(expectedOptionTemplate, CommandOptionType.NoValue);

            //Act
            descriptor.AddCommandLineParameterTo(command);

            //Assert
            var actualOption = command.Options.First();
            Assert.Equal(expectedOption.LongName, actualOption.LongName);
            Assert.Equal(expectedOption.ShortName, actualOption.ShortName);
            Assert.Equal(CommandOptionType.NoValue, actualOption.OptionType);
            Assert.Equal(expectedOptionDescription, actualOption.Description);

            //Arrange
            command.Execute(new string[0] { });

            //Assert
            Assert.Equal(falseTestDefaultValue, descriptor.Value);

            //Arrange
            command.Execute(new string[] { trueValuTestCase });

            //Assert
            Assert.Equal(true, descriptor.Value);
        }

        private class TestClass
        {
            public bool BooleanWithoutExplicitOption { get; set; }

            [Option(Description = "Bool with explicit option", Name = "NameOverride", ShortName = "bwo")]
            public bool BooleanWithExplicitOption { get; set; }

            [Option(DefaultValue = true)]
            public bool BooleanWithDefaultValue { get; set; }
        }
    }
}