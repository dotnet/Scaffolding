// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Linq;
using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Xunit;

namespace Microsoft.VisualStudio.Web.CodeGeneration.Core.Test
{
    public class ActionDescriptorTests
    {
        [Fact]
        public void ActionDescriptor_ActionModel_Returns_Correct_Type()
        {
            //Arrange
            var genDescriptorMock = GetMockDescriptor(typeof(CodeGeneratorSample).GetTypeInfo());
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
            var genDescriptorMock = GetMockDescriptor(typeof(CodeGeneratorSample).GetTypeInfo());
            var actionDescriptor = new ActionDescriptor(genDescriptorMock.Object,
                typeof(CodeGeneratorSample).GetMethod("GenerateCode"));

            //Act
            var parameters = actionDescriptor.Parameters;

            //Assert
            var propertyNames = parameters.Select(pd => pd.Property.Name);
            var expectedProperties = new[] { "BoolProperty", "StringProperty" }.ToList();
            Assert.Equal(expectedProperties, propertyNames, StringComparer.Ordinal);
        }

        private Mock<CodeGeneratorDescriptor> GetMockDescriptor(TypeInfo generatorTypeInfo)
        {
            var serviceProviderMock = new Mock<IServiceProvider>();
            return new Mock<CodeGeneratorDescriptor>(generatorTypeInfo,
                serviceProviderMock.Object);
        }

        private class CodeGeneratorSample
        {
            public void GenerateCode(CodeGeneratorModel model)
            {

            }
        }

        // This exists to ensure that properties on base class are also considered
        // for parameters.
        private class BaseModel
        {
            public string StringProperty { get; set; }
        }

        private class CodeGeneratorModel : BaseModel
        {
            public string NonWritableProperty
            {
                get
                {
                    return "";
                }
            }

            public bool BoolProperty { get; set; }

            public int IntProperty { get; set; }
        }
    }
}