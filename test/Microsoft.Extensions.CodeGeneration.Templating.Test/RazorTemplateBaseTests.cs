// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using System.Threading.Tasks;
using Xunit;

namespace Microsoft.Extensions.CodeGeneration.Templating.Test
{
    public class RazorTemplateBaseTests
    {
        [Fact]
        public async void ExecuteTemplate_Sets_Output_And_Calls_ExecuteAsync()
        {
            //Arrange
            var customInstance = new CustomClass();

            //Act
            var result = await customInstance.ExecuteTemplate();

            //Assert
            Assert.Equal("SampleText", result);
            Assert.True(customInstance.ExecuteAsyncCalled);
        }

        [Fact]
        public void WriteLiteralTo_Does_Not_Fail_For_Null_Object()
        {
            using (var writer = new StringWriter())
            {
                //Arrange
                var customInstace = new CustomClass();

                //Act
                customInstace.WriteLiteralTo(writer, null);

                //Assert
                Assert.Equal(string.Empty, writer.ToString());
            }
        }

        [Fact]
        public void WriteTo_Does_Not_Fail_For_Null_Object()
        {
            using (var writer = new StringWriter())
            {
                //Arrange
                var customInstace = new CustomClass();

                //Act
                customInstace.WriteTo(writer, null);

                //Assert
                Assert.Equal(string.Empty, writer.ToString());
            }
        }

        private class CustomClass : RazorTemplateBase
        {
            public bool ExecuteAsyncCalled { get; set; }
            public override Task ExecuteAsync()
            {
                return Task.Run(() =>
                {
                    Write("SampleText");
                    ExecuteAsyncCalled = true;
                });
            }
        }
    }
}