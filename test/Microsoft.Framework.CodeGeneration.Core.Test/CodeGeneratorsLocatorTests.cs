// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Linq;
using System.Reflection;
using Microsoft.Framework.DependencyInjection;
using Moq;
using Xunit;

namespace Microsoft.Framework.CodeGeneration.Core.Test
{
    public class CodeGeneratorsLocatorTests
    {
        Assembly currentAssembly = Assembly.GetExecutingAssembly();

        [Fact]
        public void CodeGeneratorsLocator_Returns_Correct_Number_Of_Generators()
        {
            //Arrange
            var mockServiceProvider = new Mock<IServiceProvider>();
            var mockAssemblyProvider = new Mock<ICodeGeneratorAssemblyProvider>();

            mockAssemblyProvider
                .SetupGet(ap => ap.CandidateAssemblies)
                .Returns(new[] { currentAssembly });

            var locator = new CodeGeneratorsLocator(mockServiceProvider.Object,
                mockAssemblyProvider.Object);

            //Act
            var generators = locator.CodeGenerators;

            //This is relying on types within this assembly, so a bit fragile,
            //any time a new type is added matching the naming convention, this test needs to be updated.
            //Assert
            Assert.Equal(2, generators.Count());
        }

        [Fact]
        public void CodeGeneratorsLocator_Returns_Correct_CodeGenerator_For_A_Name()
        {
            //Arrange
            var mockServiceProvider = new Mock<IServiceProvider>();
            var mockAssemblyProvider = new Mock<ICodeGeneratorAssemblyProvider>();

            mockAssemblyProvider
                .SetupGet(ap => ap.CandidateAssemblies)
                .Returns(new[] { currentAssembly });

            var locator = new CodeGeneratorsLocator(mockServiceProvider.Object,
                mockAssemblyProvider.Object);

            //Act
            var generator = locator.GetCodeGenerator("SampleCodeGenerator");

            //Assert
            Assert.NotNull(generator);
        }

        [Fact]
        public void CodeGeneratorsLocator_Throws_When_No_CodeGenerator_Found_For_A_Name()
        {
            //Arrange
            var mockServiceProvider = new Mock<IServiceProvider>();
            var mockAssemblyProvider = new Mock<ICodeGeneratorAssemblyProvider>();

            mockAssemblyProvider
                .SetupGet(ap => ap.CandidateAssemblies)
                .Returns(new[] { currentAssembly });

            var locator = new CodeGeneratorsLocator(mockServiceProvider.Object,
                mockAssemblyProvider.Object);

            //Act & Assert
            var ex = Assert.Throws<InvalidOperationException>(() => locator.GetCodeGenerator("NonExistingCodeGenerator"));
            Assert.Equal("No code generators found with the name 'NonExistingCodeGenerator'", ex.Message);
        }

        //This should be returned.
        private class SampleCodeGenerator
        {
        }

        //This should be returned.
        private class GeneratorDerivingFromInterface : ICodeGenerator
        {
        }

        //This should not be returned.
        private interface InterfaceEndingWithCodeGenerator
        {
        }

        //This should not be returned.
        private abstract class AbstractClassDerivingFromInterface : ICodeGenerator
        {
        }

        //This should not be returned.
        private class GenericClassCodeGenertor<T> where T : class
        {
        }
    }
}