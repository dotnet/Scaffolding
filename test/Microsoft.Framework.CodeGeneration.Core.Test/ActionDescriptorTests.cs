// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Linq;
using Moq;
using Xunit;

namespace Microsoft.Framework.CodeGeneration.Core.Test
{
    public class ActionDescriptorTests
    {
        [Fact]
        public void ActionDescriptor_ActionModel_Returns_Correct_Type()
        {
            //Arrange
            var genDescriptorMock = new Mock<ICodeGeneratorDescriptor>();
            var actionDescriptor = new ActionDescriptor(genDescriptorMock.Object,
                typeof(CodeGeneratorSample).GetMethod("GenerateCode"));

            //Act
            var actionModel = actionDescriptor.ActionModel;

            //Assert
            Assert.Equal(typeof(CodeGeneratorModel), actionModel);
        }

        [Fact]
        public void ActionDescriptor_Parameters_Returns_Parameters_Writeable_And_Of_Supported_Type()
        {
            //Arrange
            var genDescriptorMock = new Mock<ICodeGeneratorDescriptor>();
            var actionDescriptor = new ActionDescriptor(genDescriptorMock.Object,
                typeof(CodeGeneratorSample).GetMethod("GenerateCode"));

            //Act
            var parameters = actionDescriptor.Parameters;

            //Assert
            var propertyNames = parameters.Select(pd => pd.Property.Name);
            var expectedProperties = new[] { "StringProperty", "BoolProperty" }.ToList();
            Assert.Equal(expectedProperties, propertyNames, StringComparer.Ordinal); 
        }

        private class CodeGeneratorSample
        {
            public void GenerateCode(CodeGeneratorModel model)
            {

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