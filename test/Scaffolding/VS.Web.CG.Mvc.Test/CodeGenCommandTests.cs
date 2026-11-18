// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Reflection;
using Microsoft.DotNet.Scaffolding.Shared;
using Moq;
using Xunit;

namespace Microsoft.VisualStudio.Web.CodeGeneration
{
    public class CodeGenCommandTests
    {
        [Fact]
        public void ExecutePropagatesGeneratorFailure()
        {
            const string errorMessage = "Layout resolution failed.";
            var generator = new FailingGenerator(errorMessage);
            var serviceProvider = new Mock<IServiceProvider>();
            var descriptor = new Mock<CodeGeneratorDescriptor>(
                typeof(FailingGenerator).GetTypeInfo(),
                serviceProvider.Object);
            descriptor
                .SetupGet(value => value.CodeGeneratorInstance)
                .Returns(generator);
            descriptor
                .SetupGet(value => value.Name)
                .Returns(nameof(FailingGenerator));
            var action = new ActionDescriptor(
                descriptor.Object,
                typeof(FailingGenerator).GetMethod(nameof(FailingGenerator.GenerateCode)));
            descriptor
                .SetupGet(value => value.CodeGeneratorAction)
                .Returns(action);
            var locator = new Mock<ICodeGeneratorLocator>();
            locator
                .Setup(value => value.GetCodeGenerator(nameof(FailingGenerator)))
                .Returns(descriptor.Object);
            var command = new CodeGenCommand(
                new Mock<ILogger>().Object,
                locator.Object);

            var exception = Assert.Throws<InvalidOperationException>(() =>
                command.Execute(new[] { nameof(FailingGenerator) }));

            Assert.Equal(errorMessage, exception.Message);
        }

        private sealed class FailingGenerator
        {
            private readonly string _errorMessage;

            public FailingGenerator(string errorMessage)
            {
                _errorMessage = errorMessage;
            }

            public void GenerateCode(FailingGeneratorModel model)
            {
                throw new InvalidOperationException(_errorMessage);
            }
        }

        private sealed class FailingGeneratorModel
        {
        }
    }
}
