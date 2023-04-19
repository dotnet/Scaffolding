// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Xunit;

namespace Microsoft.VisualStudio.Web.CodeGeneration.Core.Test
{
    public class ActionInvokerTests
    {
        [Fact]
        public void ActionInvoker_Invokes_GenerateCode_Method_With_Right_ActionModel()
        {
            //Arrange
            bool methodCalled = false;
            CodeGeneratorModel invokedModel = null;

            var codeGenInstance = new CodeGeneratorSample((model) =>
            {
                methodCalled = true;
                invokedModel = model;
            });

            var serviceProviderMock = new Mock<IServiceProvider>();
            var generatorMock = new Mock<CodeGeneratorDescriptor>(typeof(CodeGeneratorSample).GetTypeInfo(),
                serviceProviderMock.Object);

            generatorMock
                .SetupGet(cd => cd.CodeGeneratorInstance)
                .Returns(codeGenInstance);
            generatorMock
                .SetupGet(cd => cd.Name)
                .Returns(typeof(CodeGeneratorSample).Name);

            var actionDescriptor = new ActionDescriptor(generatorMock.Object,
                typeof(CodeGeneratorSample).GetMethod("GenerateCode")); //This is not a perfect unit test as the arrange is using actual instance rather than a mock

            var actionInvoker = new ActionInvoker(actionDescriptor);

            //Act
            actionInvoker.Execute("CodeGeneratorSample StringValuePassed --BoolProperty".Split(' '));

            //Assert
            Assert.True(methodCalled);
            Assert.NotNull(invokedModel);
            Assert.Equal("StringValuePassed", invokedModel.StringProperty);
            Assert.True(invokedModel.BoolProperty);
        }

        [Fact]
        public void ActionInvoker_Throws_With_Inner_Exception()
        {
            //Arrange
            const string NOT_IMPLEMENTED_MESSAGE = "This action is intentionally not implemented.";
            var codeGenInstance = new CodeGeneratorSample((model) =>
            {
                throw new NotImplementedException(NOT_IMPLEMENTED_MESSAGE);
            });

            var serviceProviderMock = new Mock<IServiceProvider>();
            var generatorMock = new Mock<CodeGeneratorDescriptor>(typeof(CodeGeneratorSample).GetTypeInfo(),
                serviceProviderMock.Object);

            generatorMock
                .SetupGet(cd => cd.CodeGeneratorInstance)
                .Returns(codeGenInstance);
            generatorMock
                .SetupGet(cd => cd.Name)
                .Returns(typeof(CodeGeneratorSample).Name);

            var actionDescriptor = new ActionDescriptor(generatorMock.Object,
                typeof(CodeGeneratorSample).GetMethod("GenerateCode")); //This is not a perfect unit test as the arrange is using actual instance rather than a mock

            var actionInvoker = new ActionInvoker(actionDescriptor);

            //Act
            var ex =
                Assert.Throws<InvalidOperationException>(
                () => actionInvoker.Execute("CodeGeneratorSample StringValuePassed --BoolProperty".Split(' ')));

            //Assert
            Assert.Equal(NOT_IMPLEMENTED_MESSAGE, ex.InnerException.Message);
        }

        private class CodeGeneratorSample
        {
            private Action<CodeGeneratorModel> _generateCodeImpl;

            public CodeGeneratorSample(Action<CodeGeneratorModel> generateCodeImpl)
            {
                _generateCodeImpl = generateCodeImpl;
            }

            public void GenerateCode(CodeGeneratorModel model)
            {
                if (_generateCodeImpl != null)
                {
                    _generateCodeImpl(model);
                }
            }
        }

        private class CodeGeneratorModel
        {
            public string NonWritableProperty
            {
                get
                {
                    return "";
                }
            }

            public string StringProperty { get; set; }

            public bool BoolProperty { get; set; }

            public int IntProperty { get; set; }
        }
    }
}
