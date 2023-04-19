// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Linq;
using System.Reflection;
using Microsoft.VisualStudio.Web.CodeGeneration.CommandLine;
using Microsoft.Extensions.CommandLineUtils;
using Xunit;


namespace Microsoft.VisualStudio.Web.CodeGeneration.Core
{
    public class ParameterDescriptorTests
    {
        [Theory]
        [InlineData("BooleanWithoutExplicitOption",
                    "--BooleanWithoutExplicitOption",
                    "",
                    "--BooleanWithoutExplicitOption",
                    true,
                    false)]
        [InlineData("BooleanWithExplicitOption",
                    "--NameOverride|-bwo",
                    "Bool with explicit option",
                    "--NameOverride",
                    true,
                    false)]
        [InlineData("BooleanWithDefaultValue",
                    "--BooleanWithDefaultValue",
                    "",
                    "--BooleanWithDefaultValue",
                    true,
                    true)]
        [InlineData("StringOption",
                    "--StringOption|-so",
                    "String with explicit option",
                    "--StringOption GivenValue",
                    "GivenValue",
                    "")]
        [InlineData("StringOptionWithNameOverride",
                    "--OverridenName",
                    "",
                    "--OverridenName GivenValue",
                    "GivenValue",
                    "")]
        [InlineData("StringOptionWithDefaultValue",
                    "--StringOptionWithDefaultValue",
                    "",
                    "--StringOptionWithDefaultValue GivenValue",
                    "GivenValue",
                    "Default Value")]
        public void Options_UseCorrectName_Returns_CorrectValue(
            string propertyName,
            string expectedOptionTemplate,
            string expectedOptionDescription,
            string commandLineStringWithTheOption,
            object expectedValueWhenOptionIsPresent,
            object expectedValueWhenOptionIsNotPresent)
        {
            //Arrange
            var command = new CommandLineApplication();
            var property = typeof(TestClass).GetProperty(propertyName);
            var descriptor = new ParameterDescriptor(property);
            var optionType = expectedValueWhenOptionIsPresent is bool
                ? CommandOptionType.NoValue
                : CommandOptionType.SingleValue;
            var expectedOption = new CommandOption(expectedOptionTemplate, optionType);

            //Act
            descriptor.AddCommandLineParameterTo(command);

            //Assert
            var actualOption = command.Options.First();
            Assert.Equal(expectedOption.LongName, actualOption.LongName);
            Assert.Equal(expectedOption.ShortName, actualOption.ShortName);
            Assert.Equal(expectedOption.OptionType, actualOption.OptionType);
            Assert.Equal(expectedOptionDescription, actualOption.Description);

            //Arrange
            command.Execute(new string[0]);

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
            Assert.Null(descriptor.Value); //Is this right assumption to test?

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
