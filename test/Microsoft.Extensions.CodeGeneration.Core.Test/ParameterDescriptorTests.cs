// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Linq;
using System.Reflection;
using Microsoft.Extensions.CodeGeneration.CommandLine;
using Microsoft.Extensions.CommandLineUtils;
using Xunit;


namespace Microsoft.Extensions.CodeGeneration.Core
{
    public class ParameterDescriptorTests
    {
        [Theory]
        [InlineData("BooleanWithoutExplicitOption",
                    "--BooleanWithoutExplicitOption",
                    "",
                    CommandOptionType.NoValue,
                    "--BooleanWithoutExplicitOption",
                    true,
                    false)]
        [InlineData("BooleanWithExplicitOption",
                    "--NameOverride|-bwo",
                    "Bool with explicit option",
                    CommandOptionType.NoValue,
                    "--NameOverride",
                    true,
                    false)]
        [InlineData("BooleanWithDefaultValue",
                    "--BooleanWithDefaultValue",
                    "",
                    CommandOptionType.NoValue,
                    "--BooleanWithDefaultValue",
                    true,
                    true)]
        [InlineData("StringOption",
                    "--StringOption|-so",
                    "String with explicit option",
                    CommandOptionType.SingleValue,
                    "--StringOption GivenValue",
                    "GivenValue",
                    "")]
        [InlineData("StringOptionWithNameOverride",
                    "--OverridenName",
                    "",
                    CommandOptionType.SingleValue,
                    "--OverridenName GivenValue",
                    "GivenValue",
                    "")]
        [InlineData("StringOptionWithDefaultValue",
                    "--StringOptionWithDefaultValue",
                    "",
                    CommandOptionType.SingleValue,
                    "--StringOptionWithDefaultValue GivenValue",
                    "GivenValue",
                    "Default Value")]
        public void Options_UseCorrectName_Returns_CorrectValue(
            string propertyName,
            string expectedOptionTemplate,
            string expectedOptionDescription,
            int expectedCommandOptionType,
            string commandLineStringWithTheOption,
            object expectedValueWhenOptionIsPresent,
            object expectedValueWhenOptionIsNotPresent)
        {
            //Arrange
            var command = new CommandLineApplication();
            var property = typeof(TestClass).GetProperty(propertyName);
            var descriptor = new ParameterDescriptor(property);
            var expectedOption = new CommandOption(expectedOptionTemplate, (CommandOptionType)expectedCommandOptionType);

            //Act
            descriptor.AddCommandLineParameterTo(command);

            //Assert
            var actualOption = command.Options.First();
            Assert.Equal(expectedOption.LongName, actualOption.LongName);
            Assert.Equal(expectedOption.ShortName, actualOption.ShortName);
            Assert.Equal(expectedOption.OptionType, actualOption.OptionType);
            Assert.Equal(expectedOptionDescription, actualOption.Description);

            //Arrange
            command.Execute(new string[0] { });

            //Assert
            Assert.Equal(expectedValueWhenOptionIsNotPresent, descriptor.Value);

            //Arrange
            command.Execute(commandLineStringWithTheOption.Split(' '));

            //Assert
            Assert.Equal(expectedValueWhenOptionIsPresent, descriptor.Value);
        }

        [Theory]
        [InlineData("StringWithoutExplicitArgument", "")]
        [InlineData("StringWithExplicitArgument", "My string description")]
        public void Arguments_HaveCorrectDescription_Returns_CorrectValue(
            string propertyName,
            string expectedDescription)
        {
            //Arrange
            var command = new CommandLineApplication();
            var property = typeof(TestClass).GetProperty(propertyName);
            var descriptor = new ParameterDescriptor(property);

            //Act
            descriptor.AddCommandLineParameterTo(command);

            //Assert
            var actualOption = command.Arguments.First();
            Assert.Equal(propertyName, actualOption.Name);
            Assert.Equal(expectedDescription, actualOption.Description);

            //Arrange
            command.Execute(new string[0] { });

            //Assert
            Assert.Equal(null, descriptor.Value); //Is this right assumption to test?

            //Arrange
            command.Execute(new string[] { "PassedValue" });

            //Assert
            Assert.Equal("PassedValue", descriptor.Value);
        }

        private class TestClass
        {
            public bool BooleanWithoutExplicitOption { get; set; }

            [Option(Description = "Bool with explicit option", Name = "NameOverride", ShortName = "bwo")]
            public bool BooleanWithExplicitOption { get; set; }

            [Option(DefaultValue = true)]
            public bool BooleanWithDefaultValue { get; set; }

            [Option(Description = "String with explicit option", ShortName = "so")]
            public string StringOption { get; set; }

            [Option(Name = "OverridenName")]
            public string StringOptionWithNameOverride { get; set; }

            [Option(DefaultValue = "Default Value")]
            public string StringOptionWithDefaultValue { get; set; }

            public string StringWithoutExplicitArgument { get; set; }

            [Argument(Description = "My string description")]
            public string StringWithExplicitArgument { get; set; }
        }
    }
}