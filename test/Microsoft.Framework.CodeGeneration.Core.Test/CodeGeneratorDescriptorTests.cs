// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Reflection;
using Microsoft.Framework.CodeGeneration.CommandLine;
using Microsoft.Framework.DependencyInjection;
using Moq;
using Xunit;

namespace Microsoft.Framework.CodeGeneration.Core.Test
{
    public class CodeGeneratorDescriptorTests
    {
        [Fact]
        public void CodeGeneratorDescriptor_Name_Uses_Alias_When_Present()
        {
            //Arrange
            var mockServiceProvider = new Mock<IServiceProvider>();
            var descriptor = new CodeGeneratorDescriptor(typeof(CodeGeneratorWithAlias).GetTypeInfo(),
                mockServiceProvider.Object);

            //Act
            var name = descriptor.Name;

            //Assert
            Assert.Equal("MyAlias", name);
        }

        [Fact]
        public void CodeGeneratorDescriptor_Name_Uses_TypeName_When_Alias_Not_Present()
        {
            //Arrange
            var mockServiceProvider = new Mock<IServiceProvider>();
            var descriptor = new CodeGeneratorDescriptor(typeof(CodeGeneratorWithoutAlias).GetTypeInfo(),
                mockServiceProvider.Object);

            //Act
            var name = descriptor.Name;

            //Assert
            Assert.Equal("CodeGeneratorWithoutAlias", name);
        }

        [Fact]
        public void CodeGeneratorDescriptor_CodeGeneratorAction_Throws_When_No_GenerateCode_Method()
        {
            //Arrange
            var mockServiceProvider = new Mock<IServiceProvider>();
            var descriptor = new CodeGeneratorDescriptor(typeof(CodeGeneratorWithGenerateCodeNoParameters).GetTypeInfo(),
                mockServiceProvider.Object);

            //Act
            var ex = Assert.Throws<InvalidOperationException>(() => descriptor.CodeGeneratorAction);
            Assert.Equal("GenerateCode method with a model parameter is not found in class: " +
                "Microsoft.Framework.CodeGeneration.Core.Test.CodeGeneratorDescriptorTests+CodeGeneratorWithGenerateCodeNoParameters",
                ex.Message);
        }

        [Fact]
        public void CodeGeneratorDescriptor_CodeGeneratorAction_Throws_When_Multiple_GenerateCode_Methods()
        {
            //Arrange
            var mockServiceProvider = new Mock<IServiceProvider>();
            var descriptor = new CodeGeneratorDescriptor(typeof(ClassWithMultipleGenerateCodeMethods).GetTypeInfo(),
                mockServiceProvider.Object);

            //Act
            var ex = Assert.Throws<InvalidOperationException>(() => descriptor.CodeGeneratorAction);
            Assert.Equal("Multiple GenerateCode methods with a model parameter are found in class: " +
                "Microsoft.Framework.CodeGeneration.Core.Test.CodeGeneratorDescriptorTests+ClassWithMultipleGenerateCodeMethods",
                ex.Message);
        }

        [Fact]
        public void CodeGeneratorDescriptor_CodeGeneratorAction_Returns_Valid_GenerateCode_Method()
        {
            //Arrange
            var mockServiceProvider = new Mock<IServiceProvider>();
            var descriptor = new CodeGeneratorDescriptor(typeof(ClassWithGenerateCodeMethod).GetTypeInfo(),
                mockServiceProvider.Object);

            //Act
            var action = descriptor.CodeGeneratorAction;

            //Assert
            Assert.NotNull(action);
            Assert.Equal("GenerateCode", action.ActionMethod.Name);
        }

        [Fact]
        public void CodeGeneratorDescriptor_CodeGeneratorInstance_Injects_Dependencies()
        {
            //Arrange
            var mockServiceProvider = new Mock<IServiceProvider>();
            mockServiceProvider
                .Setup(sp => sp.GetService(typeof(IServiceProvider)))
                .Returns(mockServiceProvider.Object);

            var descriptor = new CodeGeneratorDescriptor(typeof(CodeGeneratorWithDependencies).GetTypeInfo(),
                mockServiceProvider.Object);

            //Act
            var instance = descriptor.CodeGeneratorInstance as CodeGeneratorWithDependencies;

            //Assert
            Assert.NotNull(instance);
            Assert.Equal(mockServiceProvider.Object, instance.ServiceProvider);
        }

        [Fact]
        public void CodeGeneratorDescriptor_CodeGeneratorInstance_Throws_When_Not_Able_To_Create_Instance()
        {
            //Arrange
            var mockServiceProvider = new Mock<IServiceProvider>();

            var descriptor = new CodeGeneratorDescriptor(typeof(CodeGeneratorWithDependencies).GetTypeInfo(),
                mockServiceProvider.Object);

            //Act & Assert
            var ex = Assert.Throws<InvalidOperationException>(() => descriptor.CodeGeneratorInstance);
            Assert.True(ex.Message.StartsWith("There was an error creating the code generator instance: " +
                "Microsoft.Framework.CodeGeneration.Core.Test.CodeGeneratorDescriptorTests+CodeGeneratorWithDependencies"));
        }

        [Alias("MyAlias")]
        private class CodeGeneratorWithAlias
        {
        }

        private class CodeGeneratorWithoutAlias
        {
        }

        private class CodeGeneratorWithGenerateCodeNoParameters
        {
            public void GenerateCode()
            {
            }
        }

        private class ClassWithMultipleGenerateCodeMethods
        {
            public void GenerateCode(int input)
            {
            }
            public void GenerateCode(string input)
            {
            }
        }

        private class ClassWithGenerateCodeMethod
        {
            public void GenerateCode(string input)
            {
            }
        }

        private class CodeGeneratorWithDependencies
        {
            public CodeGeneratorWithDependencies(IServiceProvider serviceProvider)
            {
                ServiceProvider = serviceProvider;
            }

            public IServiceProvider ServiceProvider { get; private set; }
        }
    }
}