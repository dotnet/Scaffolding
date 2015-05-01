// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Xunit;

namespace Microsoft.Framework.CodeGeneration.Templating.Test
{
    public class RazorTemplatingHostTests
    {
        [Fact]
        public void Constructor_Sets_Base_Engine_Properties()
        {
            //Act
            var host = new RazorTemplatingHost(typeof(MyBaseType));

            //Assert
            Assert.Equal(typeof(MyBaseType).FullName, host.DefaultBaseClass);

            //Do not change these expected values unless the method names in RazorTemplateBase
            //changed.
            Assert.Equal("ExecuteAsync", host.GeneratedClassContext.ExecuteMethodName);
            Assert.Equal("Write", host.GeneratedClassContext.WriteMethodName);
            Assert.Equal("WriteLiteral", host.GeneratedClassContext.WriteLiteralMethodName);
            Assert.Equal("WriteTo", host.GeneratedClassContext.WriteToMethodName);
            Assert.Equal("WriteLiteralTo", host.GeneratedClassContext.WriteLiteralToMethodName);
            Assert.Equal("", host.GeneratedClassContext.TemplateTypeName);
        }

        private class MyBaseType
        {
        }
    }
}