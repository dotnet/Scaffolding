// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Reflection;
using Microsoft.Framework.DependencyInjection;
using Moq;
using Xunit;

namespace Microsoft.Framework.CodeGeneration.Core.Test
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